using HrManagement.Application.Attendance.Schedules.Overrides;
using HrManagement.Domain.Attendance.Schedules;
using HrManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HrManagement.Infrastructure.Attendance.Schedules.Overrides;

public sealed class EfWorkScheduleDateOverrideManagementPersistence
    : IWorkScheduleDateOverrideManagementPersistence
{
    private readonly IDbContextFactory<HrManagementDbContext>
        _dbContextFactory;

    public EfWorkScheduleDateOverrideManagementPersistence(
        IDbContextFactory<HrManagementDbContext> dbContextFactory)
    {
        _dbContextFactory =
            dbContextFactory;
    }

    public async Task<WorkScheduleDateOverride?> GetByIdAsync(
        Guid workScheduleDateOverrideId,
        CancellationToken cancellationToken = default)
    {
        await using HrManagementDbContext dbContext =
            await _dbContextFactory
                .CreateDbContextAsync(
                    cancellationToken);

        return await dbContext
            .WorkScheduleDateOverrides
            .AsNoTracking()
            .SingleOrDefaultAsync(
                item =>
                    item.Id ==
                    workScheduleDateOverrideId,
                cancellationToken);
    }

    public async Task<WorkScheduleDateOverride?> GetByScheduleAndDateAsync(
        Guid workScheduleId,
        DateOnly workDate,
        CancellationToken cancellationToken = default)
    {
        await using HrManagementDbContext dbContext =
            await _dbContextFactory
                .CreateDbContextAsync(
                    cancellationToken);

        return await dbContext
            .WorkScheduleDateOverrides
            .AsNoTracking()
            .SingleOrDefaultAsync(
                item =>
                    item.WorkScheduleId ==
                    workScheduleId
                    && item.WorkDate ==
                    workDate,
                cancellationToken);
    }

    public async Task CreateAsync(
        WorkScheduleDateOverride item,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(
            item);

        await using HrManagementDbContext dbContext =
            await _dbContextFactory
                .CreateDbContextAsync(
                    cancellationToken);

        await dbContext
            .WorkScheduleDateOverrides
            .AddAsync(
                item,
                cancellationToken);

        await dbContext.SaveChangesAsync(
            cancellationToken);
    }

    public async Task UpdateAsync(
        WorkScheduleDateOverride item,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(
            item);

        await using HrManagementDbContext dbContext =
            await _dbContextFactory
                .CreateDbContextAsync(
                    cancellationToken);

        dbContext
            .WorkScheduleDateOverrides
            .Update(
                item);

        await dbContext.SaveChangesAsync(
            cancellationToken);
    }

    public async Task DeleteAsync(
        Guid workScheduleDateOverrideId,
        CancellationToken cancellationToken = default)
    {
        await using HrManagementDbContext dbContext =
            await _dbContextFactory
                .CreateDbContextAsync(
                    cancellationToken);

        int deleted =
            await dbContext
                .WorkScheduleDateOverrides
                .Where(
                    item =>
                        item.Id ==
                        workScheduleDateOverrideId)
                .ExecuteDeleteAsync(
                    cancellationToken);

        if (deleted != 1)
        {
            throw new InvalidOperationException(
                "Không thể xóa ngoại lệ lịch làm việc.");
        }
    }
}
