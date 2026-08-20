using HrManagement.Application.Attendance.Schedules;
using HrManagement.Domain.Attendance.Schedules;
using HrManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HrManagement.Infrastructure.Attendance.Schedules;

public sealed class EfWorkScheduleDayRepository
    : IWorkScheduleDayRepository
{
    private readonly IDbContextFactory<HrManagementDbContext>
        _dbContextFactory;

    public EfWorkScheduleDayRepository(
        IDbContextFactory<HrManagementDbContext> dbContextFactory)
    {
        _dbContextFactory =
            dbContextFactory;
    }

    public async Task<IReadOnlyList<WorkScheduleDay>>
        GetByWorkScheduleIdAsync(
            Guid workScheduleId,
            CancellationToken cancellationToken = default)
    {
        if (workScheduleId == Guid.Empty)
        {
            return [];
        }

        await using HrManagementDbContext dbContext =
            await _dbContextFactory
                .CreateDbContextAsync(
                    cancellationToken);

        return await dbContext
            .WorkScheduleDays
            .AsNoTracking()
            .Where(
                day =>
                    day.WorkScheduleId ==
                    workScheduleId)
            .OrderBy(
                day =>
                    day.DayOfWeek)
            .ThenBy(
                day =>
                    day.Id)
            .ToListAsync(
                cancellationToken);
    }
}
