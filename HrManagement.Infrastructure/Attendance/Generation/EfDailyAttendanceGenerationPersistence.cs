using HrManagement.Application.Attendance.Generation;
using HrManagement.Domain.Attendance.Records;
using HrManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HrManagement.Infrastructure.Attendance.Generation;

public sealed class EfDailyAttendanceGenerationPersistence
    : IDailyAttendanceGenerationPersistence
{
    private readonly IDbContextFactory<HrManagementDbContext>
        _dbContextFactory;

    public EfDailyAttendanceGenerationPersistence(
        IDbContextFactory<HrManagementDbContext> dbContextFactory)
    {
        _dbContextFactory =
            dbContextFactory;
    }

    public async Task<IReadOnlyList<
DailyAttendanceGenerationCandidate>>
GetCandidatesAsync(
    DateOnly workDate,
    Guid? employeeId = null,
    CancellationToken cancellationToken = default)
    {
        await using HrManagementDbContext dbContext =
            await _dbContextFactory.CreateDbContextAsync(
                cancellationToken);

        var query =
            from assignment in
                dbContext.EmployeeWorkScheduleAssignments
                    .AsNoTracking()

            join employmentPeriod in
                dbContext.EmploymentPeriods
                    .AsNoTracking()
                on assignment.EmploymentPeriodId
                equals employmentPeriod.Id

            join schedule in
                dbContext.WorkSchedules
                    .AsNoTracking()
                on assignment.WorkScheduleId
                equals schedule.Id

            where
                assignment.EffectiveFrom <=
                    workDate

                && (
                    assignment.EffectiveTo == null
                    || assignment.EffectiveTo >=
                        workDate
                )

                && employmentPeriod.EmployeeId ==
                    assignment.EmployeeId

                && employmentPeriod.StartDate <=
                    workDate

                && (
                    employmentPeriod.EndDate == null
                    || employmentPeriod.EndDate >=
                        workDate
                )

            select new
            {
                EmployeeId =
                    assignment.EmployeeId,

                EmploymentPeriodId =
                    employmentPeriod.Id,

                WorkScheduleAssignmentId =
                    assignment.Id,

                WorkScheduleId =
                    schedule.Id,

                TimeZoneId =
                    schedule.TimeZoneId
            };

        if (employeeId.HasValue)
        {
            Guid requestedEmployeeId =
                employeeId.Value;

            query =
                query.Where(
                    row =>
                        row.EmployeeId ==
                        requestedEmployeeId);
        }

        var rows =
            await query.ToArrayAsync(
                cancellationToken);

        return rows
            .OrderBy(
                row =>
                    row.EmployeeId)
            .Select(
                row =>
                    new DailyAttendanceGenerationCandidate(
                        row.EmployeeId,
                        row.EmploymentPeriodId,
                        row.WorkScheduleAssignmentId,
                        row.WorkScheduleId,
                        row.TimeZoneId))
            .ToArray();
    }

    public async Task<IReadOnlyList<Guid>>
        GetExistingEmployeeIdsAsync(
            DateOnly workDate,
            IReadOnlyCollection<Guid> employeeIds,
            CancellationToken cancellationToken = default)
    {
        if (employeeIds.Count == 0)
        {
            return Array.Empty<Guid>();
        }

        Guid[] ids =
            employeeIds
                .Where(
                    employeeId =>
                        employeeId != Guid.Empty)
                .Distinct()
                .ToArray();

        if (ids.Length == 0)
        {
            return Array.Empty<Guid>();
        }

        await using HrManagementDbContext dbContext =
            await _dbContextFactory.CreateDbContextAsync(
                cancellationToken);

        return await dbContext.AttendanceRecords
            .AsNoTracking()
            .Where(
                record =>
                    record.WorkDate ==
                        workDate
                    && ids.Contains(
                        record.EmployeeId))
            .Select(
                record =>
                    record.EmployeeId)
            .Distinct()
            .ToArrayAsync(
                cancellationToken);
    }

    public async Task AddRangeAsync(
        IReadOnlyList<AttendanceRecord> records,
        CancellationToken cancellationToken = default)
    {
        if (records.Count == 0)
        {
            return;
        }

        await using HrManagementDbContext dbContext =
            await _dbContextFactory.CreateDbContextAsync(
                cancellationToken);

        dbContext.AttendanceRecords.AddRange(
            records);

        await dbContext.SaveChangesAsync(
            cancellationToken);
    }
}
