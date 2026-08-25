using HrManagement.Application.Attendance.Timesheets;
using HrManagement.Domain.Attendance.Timesheets;
using HrManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HrManagement.Infrastructure.Attendance.Timesheets;

public sealed class EfAttendancePeriodLockPolicy
    : IAttendancePeriodLockPolicy
{
    private readonly IDbContextFactory<HrManagementDbContext>
        _dbContextFactory;

    public EfAttendancePeriodLockPolicy(
        IDbContextFactory<HrManagementDbContext> dbContextFactory)
    {
        _dbContextFactory =
            dbContextFactory;
    }

    public async Task<bool> IsLockedAsync(
        DateOnly workDate,
        CancellationToken cancellationToken = default)
    {
        if (workDate == default)
        {
            throw new ArgumentException(
                "Ngày chấm công không hợp lệ.",
                nameof(workDate));
        }

        int year =
            workDate.Year;

        int month =
            workDate.Month;

        await using HrManagementDbContext dbContext =
            await _dbContextFactory
                .CreateDbContextAsync(
                    cancellationToken);

        return await dbContext
            .TimesheetPeriods
            .AsNoTracking()
            .AnyAsync(
                period =>
                    period.Year == year
                    && period.Month == month
                    && period.Status ==
                        TimesheetPeriodStatus.Closed,
                cancellationToken);
    }
}
