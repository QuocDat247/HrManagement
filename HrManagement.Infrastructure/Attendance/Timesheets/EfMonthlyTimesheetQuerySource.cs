using HrManagement.Application.Attendance.Timesheets;
using HrManagement.Domain.Attendance.Records;
using HrManagement.Domain.Attendance.Timesheets;
using HrManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HrManagement.Infrastructure.Attendance.Timesheets;

public sealed class EfMonthlyTimesheetQuerySource
    : IMonthlyTimesheetQuerySource
{
    private readonly IDbContextFactory<HrManagementDbContext>
        _dbContextFactory;

    public EfMonthlyTimesheetQuerySource(
        IDbContextFactory<HrManagementDbContext> dbContextFactory)
    {
        _dbContextFactory =
            dbContextFactory;
    }

    public async Task<TimesheetPeriod?> GetPeriodAsync(
        int year,
        int month,
        CancellationToken cancellationToken = default)
    {
        await using HrManagementDbContext dbContext =
            await _dbContextFactory
                .CreateDbContextAsync(
                    cancellationToken);

        return await dbContext
            .TimesheetPeriods
            .AsNoTracking()
            .SingleOrDefaultAsync(
                period =>
                    period.Year == year
                    && period.Month == month,
                cancellationToken);
    }

    public async Task<IReadOnlyList<MonthlyTimesheetDayItem>>
        GetLiveItemsAsync(
            DateOnly startDate,
            DateOnly endDate,
            CancellationToken cancellationToken = default)
    {
        await using HrManagementDbContext dbContext =
            await _dbContextFactory
                .CreateDbContextAsync(
                    cancellationToken);

        List<AttendanceRecord> records =
            await dbContext
                .AttendanceRecords
                .AsNoTracking()
                .Where(
                    record =>
                        record.WorkDate >= startDate
                        && record.WorkDate <= endDate)
                .OrderBy(
                    record =>
                        record.EmployeeId)
                .ThenBy(
                    record =>
                        record.WorkDate)
                .ToListAsync(
                    cancellationToken);

        if (records.Count == 0)
        {
            return [];
        }

        Guid[] attendanceRecordIds =
            records
                .Select(
                    record =>
                        record.Id)
                .ToArray();

        Dictionary<Guid, int> correctionRevisions =
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

        return records
            .Select(
                record =>
                {
                    int correctionRevision =
                        correctionRevisions
                            .GetValueOrDefault(
                                record.Id);

                    return new MonthlyTimesheetDayItem(
                        record.Id,
                        record.EmployeeId,
                        record.WorkDate,
                        record.IsWorkingDay,
                        record.ExpectedPlannedMinutes,
                        record.Status,
                        record.WorkedMinutes,
                        record.LateMinutes,
                        record.EarlyLeaveMinutes,
                        correctionRevision);
                })
            .ToArray();
    }

    public async Task<IReadOnlyList<MonthlyTimesheetDayItem>>
        GetClosedItemsAsync(
            Guid timesheetPeriodId,
            CancellationToken cancellationToken = default)
    {
        if (timesheetPeriodId == Guid.Empty)
        {
            return [];
        }

        await using HrManagementDbContext dbContext =
            await _dbContextFactory
                .CreateDbContextAsync(
                    cancellationToken);

        return await dbContext
            .MonthlyTimesheetDaySnapshots
            .AsNoTracking()
            .Where(
                snapshot =>
                    snapshot.TimesheetPeriodId ==
                    timesheetPeriodId)
            .OrderBy(
                snapshot =>
                    snapshot.EmployeeId)
            .ThenBy(
                snapshot =>
                    snapshot.WorkDate)
            .Select(
                snapshot =>
                    new MonthlyTimesheetDayItem(
                        snapshot.AttendanceRecordId,
                        snapshot.EmployeeId,
                        snapshot.WorkDate,
                        snapshot.IsWorkingDay,
                        snapshot.ExpectedPlannedMinutes,
                        snapshot.Status,
                        snapshot.WorkedMinutes,
                        snapshot.LateMinutes,
                        snapshot.EarlyLeaveMinutes,
                        snapshot.CorrectionRevision))
            .ToListAsync(
                cancellationToken);
    }
}
