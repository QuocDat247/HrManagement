using HrManagement.Application.Authentication.Credentials;
using HrManagement.Domain.Authentication.Credentials;
using HrManagement.Domain.Authentication.Security;
using HrManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HrManagement.Infrastructure.Authentication.Credentials;

public sealed class EfAccountPasswordResetPersistence
    : IAccountPasswordResetPersistence
{
    private readonly IDbContextFactory<
        HrManagementDbContext>
        _dbContextFactory;

    public EfAccountPasswordResetPersistence(
        IDbContextFactory<HrManagementDbContext> dbContextFactory)
    {
        _dbContextFactory =
            dbContextFactory;
    }

    public async Task<bool> TryResetAsync(
        Guid accountId,
        string passwordHash,
        CancellationToken cancellationToken = default)
    {
        await using HrManagementDbContext dbContext =
            await _dbContextFactory
                .CreateDbContextAsync(
                    cancellationToken);

        bool credentialExists =
            await dbContext.UserCredentials
                .AsNoTracking()
                .AnyAsync(
                    credential =>
                        credential.AccountId ==
                        accountId,
                    cancellationToken);

        if (!credentialExists)
        {
            return false;
        }

        dbContext.UserCredentials.Update(
            new UserCredential(
                accountId,
                passwordHash,
                mustChangePassword:
                    true));

        bool securityStateExists =
            await dbContext
                .UserLoginSecurityStates
                .AsNoTracking()
                .AnyAsync(
                    state =>
                        state.AccountId ==
                        accountId,
                    cancellationToken);

        var resetSecurityState =
            new UserLoginSecurityState(
                accountId);

        if (securityStateExists)
        {
            dbContext
                .UserLoginSecurityStates
                .Update(
                    resetSecurityState);
        }
        else
        {
            await dbContext
                .UserLoginSecurityStates
                .AddAsync(
                    resetSecurityState,
                    cancellationToken);
        }

        await dbContext.SaveChangesAsync(
            cancellationToken);

        return true;
    }
}
