using HrManagement.Application.Attendance.Schedules;
using HrManagement.Domain.Attendance.Schedules;
using HrManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HrManagement.Infrastructure.Attendance.Schedules;

public sealed class EfWorkScheduleManagementPersistence
    : IWorkScheduleManagementPersistence
{
    private readonly IDbContextFactory<HrManagementDbContext>
        _dbContextFactory;

    public EfWorkScheduleManagementPersistence(
        IDbContextFactory<HrManagementDbContext> dbContextFactory)
    {
        _dbContextFactory =
            dbContextFactory;
    }

    public async Task<WorkSchedule?> GetByIdAsync(
        Guid workScheduleId,
        CancellationToken cancellationToken = default)
    {
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

    public async Task<WorkSchedule?> GetByCodeAsync(
        string code,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(
                code))
        {
            return null;
        }

        string normalizedCode =
            code.Trim()
                .ToUpperInvariant();

        await using HrManagementDbContext dbContext =
            await _dbContextFactory
                .CreateDbContextAsync(
                    cancellationToken);

        return await dbContext
            .WorkSchedules
            .AsNoTracking()
            .SingleOrDefaultAsync(
                schedule =>
                    schedule.Code ==
                    normalizedCode,
                cancellationToken);
    }

    public async Task CreateAsync(
        WorkSchedule schedule,
        IReadOnlyList<WorkScheduleDay> days,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(
            schedule);

        ArgumentNullException.ThrowIfNull(
            days);

        await using HrManagementDbContext dbContext =
            await _dbContextFactory
                .CreateDbContextAsync(
                    cancellationToken);

        await dbContext.WorkSchedules.AddAsync(
            schedule,
            cancellationToken);

        await dbContext.WorkScheduleDays.AddRangeAsync(
            days,
            cancellationToken);

        await dbContext.SaveChangesAsync(
            cancellationToken);
    }

    public async Task UpdateAsync(
        WorkSchedule schedule,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(
            schedule);

        await using HrManagementDbContext dbContext =
            await _dbContextFactory
                .CreateDbContextAsync(
                    cancellationToken);

        dbContext.WorkSchedules.Update(
            schedule);

        await dbContext.SaveChangesAsync(
            cancellationToken);
    }

    public async Task<bool> IsInUseAsync(
    Guid workScheduleId,
    CancellationToken cancellationToken = default)
    {
        await using var dbContext =
            await _dbContextFactory
                .CreateDbContextAsync(
                    cancellationToken);

        bool hasAssignments =
            await dbContext
                .EmployeeWorkScheduleAssignments
                .AsNoTracking()
                .AnyAsync(
                    assignment =>
                        assignment.WorkScheduleId ==
                        workScheduleId,
                    cancellationToken);

        if (hasAssignments)
        {
            return true;
        }

        return await dbContext
            .AttendanceRecords
            .AsNoTracking()
            .AnyAsync(
                record =>
                    record.WorkScheduleId ==
                    workScheduleId,
                cancellationToken);
    }

    public async Task DeleteAsync(
    Guid workScheduleId,
    CancellationToken cancellationToken = default)
    {
        await using var dbContext =
            await _dbContextFactory
                .CreateDbContextAsync(
                    cancellationToken);

        await using var transaction =
            await dbContext.Database
                .BeginTransactionAsync(
                    cancellationToken);

        await dbContext
            .WorkScheduleDays
            .Where(
                day =>
                    day.WorkScheduleId ==
                    workScheduleId)
            .ExecuteDeleteAsync(
                cancellationToken);

        int deletedSchedules =
            await dbContext
                .WorkSchedules
                .Where(
                    schedule =>
                        schedule.Id ==
                        workScheduleId)
                .ExecuteDeleteAsync(
                    cancellationToken);

        if (deletedSchedules != 1)
        {
            throw new InvalidOperationException(
                "Không thể xóa mẫu lịch làm việc.");
        }

        await transaction.CommitAsync(
            cancellationToken);
    }
}
