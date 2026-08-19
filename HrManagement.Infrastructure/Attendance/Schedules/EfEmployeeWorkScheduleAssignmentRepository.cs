using HrManagement.Application.Attendance.Schedules;
using HrManagement.Domain.Attendance.Schedules;
using HrManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HrManagement.Infrastructure.Attendance.Schedules;

public sealed class EfEmployeeWorkScheduleAssignmentRepository
    : IEmployeeWorkScheduleAssignmentRepository
{
    private readonly IDbContextFactory<HrManagementDbContext>
        _dbContextFactory;

    public EfEmployeeWorkScheduleAssignmentRepository(
        IDbContextFactory<HrManagementDbContext> dbContextFactory)
    {
        _dbContextFactory =
            dbContextFactory;
    }

    public async Task<IReadOnlyList<EmployeeWorkScheduleAssignment>>
        GetByEmployeeIdAsync(
            Guid employeeId,
            CancellationToken cancellationToken = default)
    {
        if (employeeId == Guid.Empty)
        {
            return [];
        }

        await using HrManagementDbContext dbContext =
            await _dbContextFactory
                .CreateDbContextAsync(
                    cancellationToken);

        return await dbContext
            .EmployeeWorkScheduleAssignments
            .AsNoTracking()
            .Where(
                assignment =>
                    assignment.EmployeeId ==
                    employeeId)
            .OrderBy(
                assignment =>
                    assignment.EffectiveFrom)
            .ThenBy(
                assignment =>
                    assignment.Id)
            .ToListAsync(
                cancellationToken);
    }
}
