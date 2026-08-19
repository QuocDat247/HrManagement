using HrManagement.Application.Attendance.Schedules;
using HrManagement.Domain.Attendance.Schedules;
using HrManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HrManagement.Infrastructure.Attendance.Schedules;

public sealed class EfWorkScheduleRepository
    : IWorkScheduleRepository
{
    private readonly IDbContextFactory<HrManagementDbContext>
        _dbContextFactory;

    public EfWorkScheduleRepository(
        IDbContextFactory<HrManagementDbContext> dbContextFactory)
    {
        _dbContextFactory =
            dbContextFactory;
    }

    public async Task<WorkSchedule?> GetByIdAsync(
        Guid workScheduleId,
        CancellationToken cancellationToken = default)
    {
        if (workScheduleId == Guid.Empty)
        {
            return null;
        }

        await using HrManagementDbContext dbContext =
            await _dbContextFactory
                .CreateDbContextAsync(
                    cancellationToken);

        return await dbContext
            .WorkSchedules
            .AsNoTracking()
            .SingleOrDefaultAsync(
                schedule =>
                    schedule.Id ==
                    workScheduleId,
                cancellationToken);
    }
}
