using System.Data;
using HrManagement.Application.Attendance.Timesheets;
using HrManagement.Application.Auditing;
using HrManagement.Domain.Attendance.Calculations;
using HrManagement.Domain.Attendance.Records;
using HrManagement.Domain.Attendance.Timesheets;
using HrManagement.Domain.Auditing;
using HrManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HrManagement.Infrastructure.Attendance.Timesheets;

public sealed class EfCloseTimesheetPeriodPersistence
    : ICloseTimesheetPeriodPersistence
{
    private readonly IDbContextFactory<HrManagementDbContext>
        _dbContextFactory;

    public EfCloseTimesheetPeriodPersistence(
        IDbContextFactory<HrManagementDbContext> dbContextFactory)
    {
        _dbContextFactory =
            dbContextFactory;
    }

    public async Task<CloseTimesheetPeriodPersistenceResult>
        CloseAsync(
            int year,
            int month,
            DateTime closedAtUtc,
            string actorUserId,
            string actorUsername,
            CancellationToken cancellationToken = default)
    {
        ValidateRequest(
            year,
            month,
            closedAtUtc,
            actorUserId,
            actorUsername);

        string normalizedActorUserId =
            actorUserId.Trim();

        string normalizedActorUsername =
            actorUsername.Trim();

        DateOnly startDate =
            new(
                year,
                month,
                1);

        DateOnly endDate =
            new(
                year,
                month,
                DateTime.DaysInMonth(
                    year,
                    month));

        await using HrManagementDbContext dbContext =
            await _dbContextFactory
                .CreateDbContextAsync(
                    cancellationToken);

        await using var transaction =
            await dbContext.Database
                .BeginTransactionAsync(
                    IsolationLevel.Serializable,
                    cancellationToken);

        TimesheetPeriod? period =
            await dbContext
                .TimesheetPeriods
                .SingleOrDefaultAsync(
                    item =>
                        item.Year == year
                        && item.Month == month,
                    cancellationToken);

        if (period is not null
            && period.IsClosed)
        {
            throw new InvalidOperationException(
                "Kỳ công đã được đóng.");
        }

        bool isNewPeriod =
            period is null;

        period ??=
            new TimesheetPeriod(
                Guid.NewGuid(),
                year,
                month);

        if (!isNewPeriod)
        {
            bool existingSnapshots =
                await dbContext
                    .MonthlyTimesheetDaySnapshots
                    .AnyAsync(
                        snapshot =>
                            snapshot.TimesheetPeriodId ==
                            period.Id,
                        cancellationToken);

            if (existingSnapshots)
            {
                throw new InvalidOperationException(
                    "Kỳ công đang mở nhưng đã tồn tại snapshot. Dữ liệu kỳ công không nhất quán.");
            }
        }

        var employmentPeriods =
            await dbContext
                .EmploymentPeriods
                .AsNoTracking()
                .Where(
                    employmentPeriod =>
                        employmentPeriod.StartDate <=
                            endDate
                        && (
                            employmentPeriod.EndDate == null
                            || employmentPeriod.EndDate >=
                                startDate
                        ))
                .Select(
                    employmentPeriod =>
                        new
                        {
                            employmentPeriod.Id,
                            employmentPeriod.EmployeeId,
                            employmentPeriod.StartDate,
                            employmentPeriod.EndDate
                        })
                .ToArrayAsync(
                    cancellationToken);

        var expectedEmploymentDays =
            new Dictionary<
                (Guid EmployeeId, DateOnly WorkDate),
                Guid>();

        foreach (var employmentPeriod
                 in employmentPeriods)
        {
            DateOnly effectiveStart =
                employmentPeriod.StartDate <
                startDate
                    ? startDate
                    : employmentPeriod.StartDate;

            DateOnly employmentEnd =
                employmentPeriod.EndDate
                ?? endDate;

            DateOnly effectiveEnd =
                employmentEnd >
                endDate
                    ? endDate
                    : employmentEnd;

            for (DateOnly workDate =
                     effectiveStart;
                 workDate <= effectiveEnd;
                 workDate =
                     workDate.AddDays(
                         1))
            {
                var key =
                    (
                        employmentPeriod.EmployeeId,
                        workDate
                    );

                if (!expectedEmploymentDays.TryAdd(
                        key,
                        employmentPeriod.Id))
                {
                    throw new InvalidOperationException(
                        "Không thể đóng kỳ công vì phát hiện nhiều giai đoạn làm việc cùng hiệu lực cho một nhân viên.");
                }
            }
        }

        var assignments =
            await dbContext
                .EmployeeWorkScheduleAssignments
                .AsNoTracking()
                .Where(
                    assignment =>
                        assignment.EffectiveFrom <=
                            endDate
                        && (
                            assignment.EffectiveTo == null
                            || assignment.EffectiveTo >=
                                startDate
                        ))
                .Select(
                    assignment =>
                        new
                        {
                            assignment.Id,
                            assignment.EmployeeId,
                            assignment.EmploymentPeriodId,
                            assignment.WorkScheduleId,
                            assignment.EffectiveFrom,
                            assignment.EffectiveTo
                        })
                .ToArrayAsync(
                    cancellationToken);

        var assignmentByDay =
            new Dictionary<
                (Guid EmployeeId, DateOnly WorkDate),
                AssignmentCoverage>();

        foreach (var assignment
                 in assignments)
        {
            DateOnly effectiveStart =
                assignment.EffectiveFrom <
                startDate
                    ? startDate
                    : assignment.EffectiveFrom;

            DateOnly assignmentEnd =
                assignment.EffectiveTo
                ?? endDate;

            DateOnly effectiveEnd =
                assignmentEnd >
                endDate
                    ? endDate
                    : assignmentEnd;

            for (DateOnly workDate =
                     effectiveStart;
                 workDate <= effectiveEnd;
                 workDate =
                     workDate.AddDays(
                         1))
            {
                var key =
                    (
                        assignment.EmployeeId,
                        workDate
                    );

                if (!expectedEmploymentDays.TryGetValue(
                        key,
                        out Guid employmentPeriodId))
                {
                    continue;
                }

                if (employmentPeriodId !=
                    assignment.EmploymentPeriodId)
                {
                    continue;
                }

                if (!assignmentByDay.TryAdd(
                        key,
                        new AssignmentCoverage(
                            assignment.Id,
                            assignment.EmploymentPeriodId,
                            assignment.WorkScheduleId)))
                {
                    throw new InvalidOperationException(
                        "Không thể đóng kỳ công vì phát hiện nhiều phân lịch làm việc cùng hiệu lực cho một nhân viên.");
                }
            }
        }

        if (assignmentByDay.Count !=
            expectedEmploymentDays.Count)
        {
            throw new InvalidOperationException(
                "Không thể đóng kỳ công vì còn ngày trong thời gian làm việc chưa có phân lịch hợp lệ.");
        }

        AttendanceRecord[] attendanceRecords =
            await dbContext
                .AttendanceRecords
                .AsNoTracking()
                .Where(
                    record =>
                        record.WorkDate >=
                            startDate
                        && record.WorkDate <=
                            endDate)
                .OrderBy(
                    record =>
                        record.EmployeeId)
                .ThenBy(
                    record =>
                        record.WorkDate)
                .ToArrayAsync(
                    cancellationToken);

        var attendanceByDay =
            new Dictionary<
                (Guid EmployeeId, DateOnly WorkDate),
                AttendanceRecord>();

        foreach (AttendanceRecord record
                 in attendanceRecords)
        {
            var key =
                (
                    record.EmployeeId,
                    record.WorkDate
                );

            if (!expectedEmploymentDays.ContainsKey(
                    key))
            {
                throw new InvalidOperationException(
                    "Không thể đóng kỳ công vì có bản ghi chấm công nằm ngoài phạm vi làm việc hợp lệ.");
            }

            if (!attendanceByDay.TryAdd(
                    key,
                    record))
            {
                throw new InvalidOperationException(
                    "Không thể đóng kỳ công vì phát hiện nhiều bản ghi chấm công cho cùng nhân viên và ngày.");
            }
        }

        if (attendanceByDay.Count !=
            expectedEmploymentDays.Count)
        {
            throw new InvalidOperationException(
                "Không thể đóng kỳ công vì dữ liệu chấm công trong tháng chưa được sinh đầy đủ.");
        }

        foreach (var expectedDay
                 in expectedEmploymentDays)
        {
            AttendanceRecord record =
                attendanceByDay[
                    expectedDay.Key];

            AssignmentCoverage assignment =
                assignmentByDay[
                    expectedDay.Key];

            if (record.EmploymentPeriodId !=
                    expectedDay.Value
                || record.WorkScheduleAssignmentId !=
                    assignment.AssignmentId
                || record.WorkScheduleId !=
                    assignment.WorkScheduleId)
            {
                throw new InvalidOperationException(
                    "Không thể đóng kỳ công vì dữ liệu chấm công không còn khớp với phân lịch làm việc.");
            }

            if (record.Status is
                AttendanceCalculationStatus.NotCalculated
                or AttendanceCalculationStatus.Incomplete)
            {
                throw new InvalidOperationException(
                    "Không thể đóng kỳ công vì vẫn còn bản ghi chấm công chưa được tính hoàn tất.");
            }
        }

        Dictionary<Guid, int>
            correctionRevisionByRecord;

        if (attendanceRecords.Length == 0)
        {
            correctionRevisionByRecord =
                [];
        }
        else
        {
            Guid[] attendanceRecordIds =
                attendanceRecords
                    .Select(
                        record =>
                            record.Id)
                    .ToArray();

            correctionRevisionByRecord =
                await dbContext
                    .AttendanceCorrections
                    .AsNoTracking()
                    .Where(
                        correction =>
                            attendanceRecordIds.Contains(
                                correction.AttendanceRecordId))
                    .GroupBy(
                        correction =>
                            correction.AttendanceRecordId)
                    .Select(
                        group =>
                            new
                            {
                                AttendanceRecordId =
                                    group.Key,

                                Revision =
                                    group.Max(
                                        correction =>
                                            correction.Revision)
                            })
                    .ToDictionaryAsync(
                        item =>
                            item.AttendanceRecordId,
                        item =>
                            item.Revision,
                        cancellationToken);
        }

        var snapshots =
            new List<MonthlyTimesheetDaySnapshot>(
                attendanceRecords.Length);

        foreach (AttendanceRecord record
                 in attendanceRecords)
        {
            int correctionRevision =
                correctionRevisionByRecord
                    .GetValueOrDefault(
                        record.Id);

            snapshots.Add(
                new MonthlyTimesheetDaySnapshot(
                    Guid.NewGuid(),
                    period.Id,
                    record.Id,
                    record.EmployeeId,
                    record.WorkDate,
                    record.IsWorkingDay,
                    record.ExpectedPlannedMinutes,
                    record.Status,
                    record.WorkedMinutes,
                    record.LateMinutes,
                    record.EarlyLeaveMinutes,
                    correctionRevision));
        }

        period.Close(
            closedAtUtc,
            normalizedActorUserId,
            normalizedActorUsername);

        var auditEntry =
            new AuditEntry(
                Guid.NewGuid(),
                closedAtUtc,
                normalizedActorUserId,
                normalizedActorUsername,
                AuditAction.Updated,
                AuditEntityTypes.TimesheetPeriod,
                period.Id);

        if (isNewPeriod)
        {
            await dbContext
                .TimesheetPeriods
                .AddAsync(
                    period,
                    cancellationToken);
        }

        if (snapshots.Count > 0)
        {
            await dbContext
                .MonthlyTimesheetDaySnapshots
                .AddRangeAsync(
                    snapshots,
                    cancellationToken);
        }

        await dbContext
            .AuditEntries
            .AddAsync(
                auditEntry,
                cancellationToken);

        try
        {
            await dbContext.SaveChangesAsync(
                cancellationToken);
        }
        catch (DbUpdateException exception)
        {
            throw new InvalidOperationException(
                "Không thể đóng kỳ công do dữ liệu đã thay đổi đồng thời. Vui lòng tải lại và thử lại.",
                exception);
        }

        await transaction.CommitAsync(
            cancellationToken);

        return new CloseTimesheetPeriodPersistenceResult(
            period.Id,
            snapshots.Count);
    }

    private static void ValidateRequest(
        int year,
        int month,
        DateTime closedAtUtc,
        string actorUserId,
        string actorUsername)
    {
        if (year < 2000
            || year > 9999)
        {
            throw new ArgumentOutOfRangeException(
                nameof(year),
                "Năm kỳ công không hợp lệ.");
        }

        if (month < 1
            || month > 12)
        {
            throw new ArgumentOutOfRangeException(
                nameof(month),
                "Tháng kỳ công phải từ 1 đến 12.");
        }

        if (closedAtUtc == default
            || closedAtUtc.Kind !=
                DateTimeKind.Utc)
        {
            throw new ArgumentException(
                "Thời điểm đóng kỳ công phải sử dụng UTC.",
                nameof(closedAtUtc));
        }

        if (string.IsNullOrWhiteSpace(
                actorUserId))
        {
            throw new ArgumentException(
                "Actor user id là bắt buộc.",
                nameof(actorUserId));
        }

        if (actorUserId.Trim().Length > 100)
        {
            throw new ArgumentException(
                "Actor user id không được vượt quá 100 ký tự.",
                nameof(actorUserId));
        }

        if (string.IsNullOrWhiteSpace(
                actorUsername))
        {
            throw new ArgumentException(
                "Actor username là bắt buộc.",
                nameof(actorUsername));
        }

        if (actorUsername.Trim().Length > 150)
        {
            throw new ArgumentException(
                "Actor username không được vượt quá 150 ký tự.",
                nameof(actorUsername));
        }
    }

    private sealed record AssignmentCoverage(
        Guid AssignmentId,
        Guid EmploymentPeriodId,
        Guid WorkScheduleId);
}
