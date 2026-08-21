using HrManagement.Application.Leave.Requests;
using HrManagement.Domain.Leave.Requests;
using HrManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HrManagement.Infrastructure.Leave.Requests;

public sealed class EfLeaveRequestStatusHistoryRepository
    : ILeaveRequestStatusHistoryRepository
{
    private readonly IDbContextFactory<HrManagementDbContext>
        _dbContextFactory;

    public EfLeaveRequestStatusHistoryRepository(
        IDbContextFactory<HrManagementDbContext> dbContextFactory)
    {
        _dbContextFactory =
            dbContextFactory;
    }

    public async Task<IReadOnlyList<LeaveRequestStatusChange>>
        GetByLeaveRequestIdAsync(
            Guid leaveRequestId,
            CancellationToken cancellationToken = default)
    {
        if (leaveRequestId == Guid.Empty)
        {
            return [];
        }

        await using HrManagementDbContext dbContext =
            await _dbContextFactory
                .CreateDbContextAsync(
                    cancellationToken);

        return await dbContext
            .LeaveRequestStatusChanges
            .AsNoTracking()
            .Where(
                change =>
                    change.LeaveRequestId ==
                    leaveRequestId)
            .OrderBy(
                change =>
                    change.ChangedAtUtc)
            .ThenBy(
                change =>
                    change.Id)
            .ToListAsync(
                cancellationToken);
    }
}
