using HrManagement.Application.Authentication.Recovery;
using HrManagement.Domain.Authentication.Accounts;
using HrManagement.Domain.Authentication.Security;
using HrManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HrManagement.Infrastructure.Authentication.Recovery;

public sealed class EfOwnerPasswordRecoveryPersistence
    : IOwnerPasswordRecoveryPersistence
{
    private readonly IDbContextFactory<
        HrManagementDbContext>
        _dbContextFactory;

    public EfOwnerPasswordRecoveryPersistence(
        IDbContextFactory<HrManagementDbContext> dbContextFactory)
    {
        _dbContextFactory =
            dbContextFactory;
    }

    public async Task<string?> GetRecoveryCodeHashAsync(
        Guid accountId,
        CancellationToken cancellationToken = default)
    {
        if (accountId == Guid.Empty)
        {
            return null;
        }

        await using HrManagementDbContext dbContext =
            await _dbContextFactory
                .CreateDbContextAsync(
                    cancellationToken);

        return await dbContext
            .OwnerRecoveryCredentials
            .AsNoTracking()
            .Where(
                credential =>
                    credential.AccountId ==
                    accountId)
            .Select(
                credential =>
                    credential.RecoveryCodeHash)
            .SingleOrDefaultAsync(
                cancellationToken);
    }

    public async Task<bool> TryRecoverAsync(
        Guid accountId,
        string expectedRecoveryCodeHash,
        string newPasswordHash,
        string newRecoveryCodeHash,
        CancellationToken cancellationToken = default)
    {
        if (accountId == Guid.Empty)
        {
            return false;
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(
            expectedRecoveryCodeHash);

        ArgumentException.ThrowIfNullOrWhiteSpace(
            newPasswordHash);

        ArgumentException.ThrowIfNullOrWhiteSpace(
            newRecoveryCodeHash);

        await using HrManagementDbContext dbContext =
            await _dbContextFactory
                .CreateDbContextAsync(
                    cancellationToken);

        await using var transaction =
            await dbContext.Database
                .BeginTransactionAsync(
                    cancellationToken);

        bool ownerExists =
            await dbContext.UserAccounts
                .AsNoTracking()
                .AnyAsync(
                    account =>
                        account.Id ==
                            accountId
                        && account.Kind ==
                            UserAccountKind.Owner
                        && account.IsActive,
                    cancellationToken);

        if (!ownerExists)
        {
            await transaction.RollbackAsync(
                cancellationToken);

            return false;
        }

        int rotatedRecoveryCredential =
            await dbContext
                .OwnerRecoveryCredentials
                .Where(
                    credential =>
                        credential.AccountId ==
                            accountId
                        && credential.RecoveryCodeHash ==
                            expectedRecoveryCodeHash)
                .ExecuteUpdateAsync(
                    setters =>
                        setters.SetProperty(
                            credential =>
                                credential.RecoveryCodeHash,
                            newRecoveryCodeHash),
                    cancellationToken);

        if (rotatedRecoveryCredential != 1)
        {
            await transaction.RollbackAsync(
                cancellationToken);

            return false;
        }

        int updatedCredential =
            await dbContext.UserCredentials
                .Where(
                    credential =>
                        credential.AccountId ==
                        accountId)
                .ExecuteUpdateAsync(
                    setters =>
                        setters
                            .SetProperty(
                                credential =>
                                    credential.PasswordHash,
                                newPasswordHash)
                            .SetProperty(
                                credential =>
                                    credential.MustChangePassword,
                                false),
                    cancellationToken);

        if (updatedCredential != 1)
        {
            await transaction.RollbackAsync(
                cancellationToken);

            return false;
        }

        int resetSecurityState =
            await dbContext
                .UserLoginSecurityStates
                .Where(
                    state =>
                        state.AccountId ==
                        accountId)
                .ExecuteUpdateAsync(
                    setters =>
                        setters
                            .SetProperty(
                                state =>
                                    state.FailedLoginCount,
                                0)
                            .SetProperty(
                                state =>
                                    state.LockoutEndUtc,
                                (DateTimeOffset?)null),
                    cancellationToken);

        if (resetSecurityState == 0)
        {
            await dbContext
                .UserLoginSecurityStates
                .AddAsync(
                    new UserLoginSecurityState(
                        accountId),
                    cancellationToken);

            await dbContext.SaveChangesAsync(
                cancellationToken);
        }

        await transaction.CommitAsync(
            cancellationToken);

        return true;
    }
}
