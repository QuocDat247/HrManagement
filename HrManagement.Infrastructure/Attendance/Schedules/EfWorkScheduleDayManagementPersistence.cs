using HrManagement.Application.Attendance.Schedules;
using HrManagement.Domain.Attendance.Schedules;
using HrManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HrManagement.Infrastructure.Attendance.Schedules;

public sealed class EfWorkScheduleDayManagementPersistence
    : IWorkScheduleDayManagementPersistence
{
    private readonly IDbContextFactory<HrManagementDbContext>
        _dbContextFactory;

    public EfWorkScheduleDayManagementPersistence(
        IDbContextFactory<HrManagementDbContext> dbContextFactory)
    {
        _dbContextFactory =
            dbContextFactory;
    }

    public async Task<WorkScheduleDay?> GetAsync(
        Guid workScheduleId,
        DayOfWeek dayOfWeek,
        CancellationToken cancellationToken = default)
    {
        if (workScheduleId ==
            Guid.Empty)
        {
            return null;
        }

        await using HrManagementDbContext dbContext =
            await _dbContextFactory
                .CreateDbContextAsync(
                    cancellationToken);

        return await dbContext
            .WorkScheduleDays
            .AsNoTracking()
            .SingleOrDefaultAsync(
                day =>
                    day.WorkScheduleId ==
                        workScheduleId
                    && day.DayOfWeek ==
                        dayOfWeek,
                cancellationToken);
    }

    public async Task UpdateAsync(
        WorkScheduleDay day,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(
            day);

        await using HrManagementDbContext dbContext =
            await _dbContextFactory
                .CreateDbContextAsync(
                    cancellationToken);

        dbContext.WorkScheduleDays.Update(
            day);

        await dbContext.SaveChangesAsync(
            cancellationToken);
    }
}
