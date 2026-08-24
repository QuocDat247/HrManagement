using HrManagement.Application.Attendance.Calculations;
using HrManagement.Domain.Attendance.Calculations;
using HrManagement.Domain.Attendance.Records;
using HrManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HrManagement.Infrastructure.Attendance.Calculations;

public sealed class EfAttendanceCalculationPersistence
    : IAttendanceCalculationPersistence
{
    private readonly IDbContextFactory<HrManagementDbContext>
        _dbContextFactory;

    public EfAttendanceCalculationPersistence(
        IDbContextFactory<HrManagementDbContext> dbContextFactory)
    {
        _dbContextFactory =
            dbContextFactory;
    }

    public async Task ApplyAsync(
    AttendanceRecord calculatedRecord,
    IReadOnlyList<AttendanceEvent> expectedEvents,
    int expectedCorrectionRevision,
    CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(
            calculatedRecord);

        ArgumentNullException.ThrowIfNull(
            expectedEvents);

        if (expectedCorrectionRevision < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(expectedCorrectionRevision));
        }

        ValidateRequest(
            calculatedRecord,
            expectedEvents);

        await using HrManagementDbContext dbContext =
            await _dbContextFactory
                .CreateDbContextAsync(
                    cancellationToken);

        await using var transaction =
            await dbContext.Database
                .BeginTransactionAsync(
                    cancellationToken);

        AttendanceRecord? persistedRecord =
            await dbContext
                .AttendanceRecords
                .AsNoTracking()
                .SingleOrDefaultAsync(
                    record =>
                        record.Id ==
                        calculatedRecord.Id,
                    cancellationToken);

        ValidatePersistedRecord(
            persistedRecord,
            calculatedRecord);

        List<AttendanceEvent> persistedEvents =
            await dbContext
                .AttendanceEvents
                .AsNoTracking()
                .Where(
                    attendanceEvent =>
                        attendanceEvent.AttendanceRecordId ==
                        calculatedRecord.Id)
                .OrderBy(
                    attendanceEvent =>
                        attendanceEvent.OccurredAtUtc)
                .ThenBy(
                    attendanceEvent =>
                        attendanceEvent.Id)
                .ToListAsync(
                    cancellationToken);

        ValidateExpectedEvents(
            persistedEvents,
            expectedEvents);

        int persistedCorrectionRevision =
            await dbContext
                .AttendanceCorrections
                .Where(
                    correction =>
                        correction.AttendanceRecordId ==
                        calculatedRecord.Id)
                .Select(
                    correction =>
                        (int?)correction.Revision)
                .MaxAsync(
                    cancellationToken)
            ?? 0;

        int persistedCorrectionCount =
            await dbContext
                .AttendanceCorrections
                .CountAsync(
                    correction =>
                        correction.AttendanceRecordId ==
                        calculatedRecord.Id,
                    cancellationToken);

        if (persistedCorrectionRevision !=
                expectedCorrectionRevision
            || persistedCorrectionCount !=
                expectedCorrectionRevision)
        {
            throw new DbUpdateConcurrencyException(
                "Lịch sử điều chỉnh chấm công đã thay đổi trước khi lưu kết quả tính công.");
        }

        int updatedRows =
            await dbContext
                .AttendanceRecords
                .Where(
                    record =>
                        record.Id ==
                            calculatedRecord.Id
                        && record.EmployeeId ==
                            calculatedRecord.EmployeeId
                        && record.EmploymentPeriodId ==
                            calculatedRecord.EmploymentPeriodId
                        && record.WorkScheduleAssignmentId ==
                            calculatedRecord.WorkScheduleAssignmentId
                        && record.WorkScheduleId ==
                            calculatedRecord.WorkScheduleId
                        && record.WorkDate ==
                            calculatedRecord.WorkDate
                        && record.TimeZoneId ==
                            calculatedRecord.TimeZoneId
                        && record.IsWorkingDay ==
                            calculatedRecord.IsWorkingDay
                        && record.ExpectedStartTime ==
                            calculatedRecord.ExpectedStartTime
                        && record.ExpectedEndTime ==
                            calculatedRecord.ExpectedEndTime
                        && record.ExpectedBreakMinutes ==
                            calculatedRecord.ExpectedBreakMinutes)
                .ExecuteUpdateAsync(
                    setters =>
                        setters
                            .SetProperty(
                                record =>
                                    record.Status,
                                calculatedRecord.Status)
                            .SetProperty(
                                record =>
                                    record.WorkedMinutes,
                                calculatedRecord.WorkedMinutes)
                            .SetProperty(
                                record =>
                                    record.LateMinutes,
                                calculatedRecord.LateMinutes)
                            .SetProperty(
                                record =>
                                    record.EarlyLeaveMinutes,
                                calculatedRecord.EarlyLeaveMinutes),
                    cancellationToken);

        if (updatedRows != 1)
        {
            throw new DbUpdateConcurrencyException(
                "Bản ghi chấm công đã thay đổi trước khi lưu kết quả tính công.");
        }

        await transaction.CommitAsync(
            cancellationToken);
    }

    private static void ValidateRequest(
        AttendanceRecord calculatedRecord,
        IReadOnlyList<AttendanceEvent> expectedEvents)
    {
        if (calculatedRecord.Status ==
            AttendanceCalculationStatus.NotCalculated)
        {
            throw new ArgumentException(
                "Bản ghi chưa có kết quả tính công.",
                nameof(calculatedRecord));
        }

        foreach (AttendanceEvent attendanceEvent
                 in expectedEvents)
        {
            if (attendanceEvent.AttendanceRecordId !=
                calculatedRecord.Id)
            {
                throw new ArgumentException(
                    "Sự kiện dự kiến không thuộc bản ghi tính công.",
                    nameof(expectedEvents));
            }

            if (attendanceEvent.EmployeeId !=
                calculatedRecord.EmployeeId)
            {
                throw new ArgumentException(
                    "Sự kiện dự kiến không thuộc nhân viên của bản ghi.",
                    nameof(expectedEvents));
            }
        }

        AttendancePunchSequencePolicy
            .EnsureValidTimeline(
                expectedEvents);
    }

    private static void ValidatePersistedRecord(
        AttendanceRecord? persisted,
        AttendanceRecord calculatedRecord)
    {
        if (persisted is null)
        {
            throw new DbUpdateConcurrencyException(
                "Không còn tìm thấy bản ghi chấm công.");
        }

        if (persisted.EmployeeId !=
                calculatedRecord.EmployeeId
            || persisted.EmploymentPeriodId !=
                calculatedRecord.EmploymentPeriodId
            || persisted.WorkScheduleAssignmentId !=
                calculatedRecord.WorkScheduleAssignmentId
            || persisted.WorkScheduleId !=
                calculatedRecord.WorkScheduleId
            || persisted.WorkDate !=
                calculatedRecord.WorkDate
            || persisted.TimeZoneId !=
                calculatedRecord.TimeZoneId
            || persisted.IsWorkingDay !=
                calculatedRecord.IsWorkingDay
            || persisted.ExpectedStartTime !=
                calculatedRecord.ExpectedStartTime
            || persisted.ExpectedEndTime !=
                calculatedRecord.ExpectedEndTime
            || persisted.ExpectedBreakMinutes !=
                calculatedRecord.ExpectedBreakMinutes)
        {
            throw new DbUpdateConcurrencyException(
                "Schedule snapshot của bản ghi chấm công không còn khớp dữ liệu đã đọc.");
        }
    }

    private static void ValidateExpectedEvents(
        IReadOnlyList<AttendanceEvent> persistedEvents,
        IReadOnlyList<AttendanceEvent> expectedEvents)
    {
        if (persistedEvents.Count !=
            expectedEvents.Count)
        {
            throw new DbUpdateConcurrencyException(
                "Lịch sử punch đã thay đổi trước khi lưu kết quả tính công.");
        }

        for (int index = 0;
             index < persistedEvents.Count;
             index++)
        {
            AttendanceEvent persisted =
                persistedEvents[index];

            AttendanceEvent expected =
                expectedEvents[index];

            if (persisted.Id != expected.Id
                || persisted.AttendanceRecordId !=
                    expected.AttendanceRecordId
                || persisted.EmployeeId !=
                    expected.EmployeeId
                || persisted.EventType !=
                    expected.EventType
                || persisted.OccurredAtUtc !=
                    expected.OccurredAtUtc)
            {
                throw new DbUpdateConcurrencyException(
                    "Lịch sử punch đã thay đổi trước khi lưu kết quả tính công.");
            }
        }
    }
}
