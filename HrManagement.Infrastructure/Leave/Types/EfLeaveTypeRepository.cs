using HrManagement.Application.Leave.Types;
using HrManagement.Domain.Leave.Types;
using HrManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HrManagement.Infrastructure.Leave.Types;

public sealed class EfLeaveTypeRepository
    : ILeaveTypeRepository
{
    private readonly IDbContextFactory<HrManagementDbContext>
        _dbContextFactory;

    public EfLeaveTypeRepository(
        IDbContextFactory<HrManagementDbContext> dbContextFactory)
    {
        _dbContextFactory =
            dbContextFactory;
    }

    public async Task<LeaveType?> GetByIdAsync(
        Guid leaveTypeId,
        CancellationToken cancellationToken = default)
    {
        if (leaveTypeId == Guid.Empty)
        {
            return null;
        }

        await using HrManagementDbContext dbContext =
            await _dbContextFactory
                .CreateDbContextAsync(
                    cancellationToken);

        return await dbContext
            .LeaveTypes
            .AsNoTracking()
            .SingleOrDefaultAsync(
                leaveType =>
                    leaveType.Id ==
                    leaveTypeId,
                cancellationToken);
    }
}
