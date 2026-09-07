using HrManagement.Application.Authentication.Accounts;
using HrManagement.Domain.Authentication.Accounts;
using HrManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HrManagement.Infrastructure.Authentication.Accounts;

public sealed class EfUserAccountRepository
    : IUserAccountRepository
{
    private readonly IDbContextFactory<HrManagementDbContext>
        _dbContextFactory;

    public EfUserAccountRepository(
        IDbContextFactory<HrManagementDbContext> dbContextFactory)
    {
        _dbContextFactory =
            dbContextFactory;
    }

    public async Task<IReadOnlyList<UserAccount>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        await using HrManagementDbContext dbContext =
            await _dbContextFactory.CreateDbContextAsync(
                cancellationToken);

        return await dbContext.UserAccounts
            .AsNoTracking()
            .OrderBy(account =>
                account.Username)
            .ToListAsync(
                cancellationToken);
    }

    public async Task<UserAccount?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        await using HrManagementDbContext dbContext =
            await _dbContextFactory.CreateDbContextAsync(
                cancellationToken);

        return await dbContext.UserAccounts
            .AsNoTracking()
            .FirstOrDefaultAsync(
                account =>
                    account.Id == id,
                cancellationToken);
    }

    public async Task<UserAccount?> GetByUsernameAsync(
        string username,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(
                username))
        {
            return null;
        }

        string normalizedUsername =
            username.Trim()
                .ToUpperInvariant();

        await using HrManagementDbContext dbContext =
            await _dbContextFactory.CreateDbContextAsync(
                cancellationToken);

        return await dbContext.UserAccounts
            .AsNoTracking()
            .FirstOrDefaultAsync(
                account =>
                    account.NormalizedUsername ==
                    normalizedUsername,
                cancellationToken);
    }

    public async Task AddAsync(
        UserAccount account,
        CancellationToken cancellationToken = default)
    {
        await using HrManagementDbContext dbContext =
            await _dbContextFactory.CreateDbContextAsync(
                cancellationToken);

        await dbContext.UserAccounts.AddAsync(
            account,
            cancellationToken);

        await dbContext.SaveChangesAsync(
            cancellationToken);
    }

    public async Task UpdateAsync(
        UserAccount account,
        CancellationToken cancellationToken = default)
    {
        await using HrManagementDbContext dbContext =
            await _dbContextFactory.CreateDbContextAsync(
                cancellationToken);

        dbContext.UserAccounts.Update(
            account);

        await dbContext.SaveChangesAsync(
            cancellationToken);
    }
}
