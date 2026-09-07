using HrManagement.Application.Authentication.Security;
using HrManagement.Domain.Authentication.Security;
using HrManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HrManagement.Infrastructure.Authentication.Security;

public sealed class EfUserLoginSecurityStateRepository
    : IUserLoginSecurityStateRepository
{
    private readonly IDbContextFactory<HrManagementDbContext>
        _dbContextFactory;

    public EfUserLoginSecurityStateRepository(
        IDbContextFactory<HrManagementDbContext> dbContextFactory)
    {
        _dbContextFactory =
            dbContextFactory;
    }

    public async Task<UserLoginSecurityState?>
        GetByAccountIdAsync(
            Guid accountId,
            CancellationToken cancellationToken = default)
    {
        await using HrManagementDbContext dbContext =
            await _dbContextFactory.CreateDbContextAsync(
                cancellationToken);

        return await dbContext.UserLoginSecurityStates
            .AsNoTracking()
            .FirstOrDefaultAsync(
                state =>
                    state.AccountId ==
                    accountId,
                cancellationToken);
    }

    public async Task AddAsync(
        UserLoginSecurityState state,
        CancellationToken cancellationToken = default)
    {
        await using HrManagementDbContext dbContext =
            await _dbContextFactory.CreateDbContextAsync(
                cancellationToken);

        await dbContext.UserLoginSecurityStates
            .AddAsync(
                state,
                cancellationToken);

        await dbContext.SaveChangesAsync(
            cancellationToken);
    }

    public async Task UpdateAsync(
        UserLoginSecurityState state,
        CancellationToken cancellationToken = default)
    {
        await using HrManagementDbContext dbContext =
            await _dbContextFactory.CreateDbContextAsync(
                cancellationToken);

        dbContext.UserLoginSecurityStates.Update(
            state);

        await dbContext.SaveChangesAsync(
            cancellationToken);
    }
}
