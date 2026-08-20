using HrManagement.Application.Leave.Requests;
using HrManagement.Domain.Leave.Requests;
using HrManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HrManagement.Infrastructure.Leave.Requests;

public sealed class EfLeaveRequestRepository
    : ILeaveRequestRepository
{
    private readonly IDbContextFactory<HrManagementDbContext>
        _dbContextFactory;

    public EfLeaveRequestRepository(
        IDbContextFactory<HrManagementDbContext> dbContextFactory)
    {
        _dbContextFactory =
            dbContextFactory;
    }

    public async Task<LeaveRequest?> GetByIdAsync(
        Guid leaveRequestId,
        CancellationToken cancellationToken = default)
    {
        if (leaveRequestId == Guid.Empty)
        {
            return null;
        }

        await using HrManagementDbContext dbContext =
            await _dbContextFactory
                .CreateDbContextAsync(
                    cancellationToken);

        return await dbContext
            .LeaveRequests
            .AsNoTracking()
            .SingleOrDefaultAsync(
                request =>
                    request.Id ==
                    leaveRequestId,
                cancellationToken);
    }

    public async Task<IReadOnlyList<LeaveRequest>>
        GetOverlappingByEmployeeAsync(
            Guid employeeId,
            DateOnly startDate,
            DateOnly endDate,
            CancellationToken cancellationToken = default)
    {
        if (employeeId == Guid.Empty
            || startDate == default
            || endDate == default
            || endDate < startDate)
        {
            return [];
        }

        await using HrManagementDbContext dbContext =
            await _dbContextFactory
                .CreateDbContextAsync(
                    cancellationToken);

        return await dbContext
            .LeaveRequests
            .AsNoTracking()
            .Where(
                request =>
                    request.EmployeeId ==
                        employeeId
                    && request.StartDate <=
                        endDate
                    && startDate <=
                        request.EndDate)
            .OrderBy(
                request =>
                    request.StartDate)
            .ThenBy(
                request =>
                    request.EndDate)
            .ThenBy(
                request =>
                    request.SubmittedAtUtc)
            .ThenBy(
                request =>
                    request.Id)
            .ToListAsync(
                cancellationToken);
    }
}
