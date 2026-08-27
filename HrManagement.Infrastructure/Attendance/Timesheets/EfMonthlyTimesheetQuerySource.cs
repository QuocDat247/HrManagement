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

        Dictionary<Guid, EmployeeIdentity>
            employeeIdentities =
                await LoadEmployeeIdentitiesAsync(
                    dbContext,
                    records.Select(
                        record =>
                            record.EmployeeId),
                    cancellationToken);

        return records
            .Select(
                record =>
                {
                    int correctionRevision =
                        correctionRevisions
                            .GetValueOrDefault(
                                record.Id);

                    EmployeeIdentity employeeIdentity =
                        GetRequiredEmployeeIdentity(
                            employeeIdentities,
                            record.EmployeeId);

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
                        correctionRevision,
                        employeeIdentity.EmployeeCode,
                        employeeIdentity.FullName);
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

        List<MonthlyTimesheetDaySnapshot> snapshots =
    await dbContext
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
        .ToListAsync(
            cancellationToken);

        if (snapshots.Count == 0)
        {
            return [];
        }

        Dictionary<Guid, EmployeeIdentity>
            employeeIdentities =
                await LoadEmployeeIdentitiesAsync(
                    dbContext,
                    snapshots.Select(
                        snapshot =>
                            snapshot.EmployeeId),
                    cancellationToken);

        return snapshots
            .Select(
                snapshot =>
                {
                    EmployeeIdentity employeeIdentity =
                        GetRequiredEmployeeIdentity(
                            employeeIdentities,
                            snapshot.EmployeeId);

                    return new MonthlyTimesheetDayItem(
                        snapshot.AttendanceRecordId,
                        snapshot.EmployeeId,
                        snapshot.WorkDate,
                        snapshot.IsWorkingDay,
                        snapshot.ExpectedPlannedMinutes,
                        snapshot.Status,
                        snapshot.WorkedMinutes,
                        snapshot.LateMinutes,
                        snapshot.EarlyLeaveMinutes,
                        snapshot.CorrectionRevision,
                        employeeIdentity.EmployeeCode,
                        employeeIdentity.FullName);
                })
            .ToArray();
    }

    private static async Task<
    Dictionary<Guid, EmployeeIdentity>>
    LoadEmployeeIdentitiesAsync(
        HrManagementDbContext dbContext,
        IEnumerable<Guid> employeeIds,
        CancellationToken cancellationToken)
    {
        Guid[] distinctEmployeeIds =
            employeeIds
                .Distinct()
                .ToArray();

        if (distinctEmployeeIds.Length == 0)
        {
            return [];
        }

        return await dbContext
            .Employees
            .AsNoTracking()
            .Where(
                employee =>
                    distinctEmployeeIds.Contains(
                        employee.Id))
            .Select(
                employee =>
                    new EmployeeIdentity(
                        employee.Id,
                        employee.EmployeeCode,
                        employee.FullName))
            .ToDictionaryAsync(
                employee =>
                    employee.Id,
                cancellationToken);
    }

    private static EmployeeIdentity
        GetRequiredEmployeeIdentity(
            IReadOnlyDictionary<Guid, EmployeeIdentity>
                employeeIdentities,
            Guid employeeId)
    {
        if (!employeeIdentities.TryGetValue(
                employeeId,
                out EmployeeIdentity? employeeIdentity))
        {
            throw new InvalidOperationException(
                $"Không tìm thấy nhân viên của dòng bảng công: {employeeId}.");
        }

        return employeeIdentity;
    }

    private sealed record EmployeeIdentity(
        Guid Id,
        string EmployeeCode,
        string FullName);
}
