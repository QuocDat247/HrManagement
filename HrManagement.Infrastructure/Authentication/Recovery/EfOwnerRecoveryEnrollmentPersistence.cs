using HrManagement.Application.Authentication.Recovery;
using HrManagement.Domain.Authentication.Recovery;
using HrManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HrManagement.Infrastructure.Authentication.Recovery;

public sealed class EfOwnerRecoveryEnrollmentPersistence
    : IOwnerRecoveryEnrollmentPersistence
{
    private readonly IDbContextFactory<
        HrManagementDbContext>
        _dbContextFactory;

    public EfOwnerRecoveryEnrollmentPersistence(
        IDbContextFactory<HrManagementDbContext> dbContextFactory)
    {
        _dbContextFactory =
            dbContextFactory;
    }

    public async Task<bool> ExistsAsync(
        Guid accountId,
        CancellationToken cancellationToken = default)
    {
        if (accountId == Guid.Empty)
        {
            return false;
        }

        await using HrManagementDbContext dbContext =
            await _dbContextFactory
                .CreateDbContextAsync(
                    cancellationToken);

        return await dbContext
            .OwnerRecoveryCredentials
            .AsNoTracking()
            .AnyAsync(
                credential =>
                    credential.AccountId ==
                    accountId,
                cancellationToken);
    }

    public async Task<bool> TryCreateAsync(
        OwnerRecoveryCredential credential,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(
            credential);

        await using HrManagementDbContext dbContext =
            await _dbContextFactory
                .CreateDbContextAsync(
                    cancellationToken);

        bool exists =
            await dbContext
                .OwnerRecoveryCredentials
                .AsNoTracking()
                .AnyAsync(
                    item =>
                        item.AccountId ==
                        credential.AccountId,
                    cancellationToken);

        if (exists)
        {
            return false;
        }

        await dbContext
            .OwnerRecoveryCredentials
            .AddAsync(
                credential,
                cancellationToken);

        try
        {
            await dbContext.SaveChangesAsync(
                cancellationToken);

            return true;
        }
        catch (DbUpdateException)
        {
            await using HrManagementDbContext
                verificationContext =
                    await _dbContextFactory
                        .CreateDbContextAsync(
                            cancellationToken);

            bool nowExists =
                await verificationContext
                    .OwnerRecoveryCredentials
                    .AsNoTracking()
                    .AnyAsync(
                        item =>
                            item.AccountId ==
                            credential.AccountId,
                        cancellationToken);

            if (nowExists)
            {
                return false;
            }

            throw;
        }
    }
}
