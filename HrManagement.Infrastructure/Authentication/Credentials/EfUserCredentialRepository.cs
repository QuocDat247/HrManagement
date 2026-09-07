using HrManagement.Application.Authentication.Credentials;
using HrManagement.Domain.Authentication.Credentials;
using HrManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HrManagement.Infrastructure.Authentication.Credentials;

public sealed class EfUserCredentialRepository
    : IUserCredentialRepository
{
    private readonly IDbContextFactory<HrManagementDbContext>
        _dbContextFactory;

    public EfUserCredentialRepository(
        IDbContextFactory<HrManagementDbContext> dbContextFactory)
    {
        _dbContextFactory =
            dbContextFactory;
    }

    public async Task<UserCredential?> GetByAccountIdAsync(
        Guid accountId,
        CancellationToken cancellationToken = default)
    {
        await using HrManagementDbContext dbContext =
            await _dbContextFactory.CreateDbContextAsync(
                cancellationToken);

        return await dbContext.UserCredentials
            .AsNoTracking()
            .FirstOrDefaultAsync(
                credential =>
                    credential.AccountId ==
                    accountId,
                cancellationToken);
    }

    public async Task AddAsync(
        UserCredential credential,
        CancellationToken cancellationToken = default)
    {
        await using HrManagementDbContext dbContext =
            await _dbContextFactory.CreateDbContextAsync(
                cancellationToken);

        await dbContext.UserCredentials.AddAsync(
            credential,
            cancellationToken);

        await dbContext.SaveChangesAsync(
            cancellationToken);
    }

    public async Task UpdateAsync(
        UserCredential credential,
        CancellationToken cancellationToken = default)
    {
        await using HrManagementDbContext dbContext =
            await _dbContextFactory.CreateDbContextAsync(
                cancellationToken);

        dbContext.UserCredentials.Update(
            credential);

        await dbContext.SaveChangesAsync(
            cancellationToken);
    }
}
