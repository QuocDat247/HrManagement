using HrManagement.Application.Authentication.Bootstrap;
using HrManagement.Domain.Authentication.Accounts;
using HrManagement.Domain.Authentication.Credentials;
using HrManagement.Domain.Authentication.Security;
using HrManagement.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace HrManagement.Infrastructure.Authentication.Bootstrap;

public sealed class EfInitialOwnerBootstrapPersistence
    : IInitialOwnerBootstrapPersistence
{
    private readonly IDbContextFactory<HrManagementDbContext>
        _dbContextFactory;

    public EfInitialOwnerBootstrapPersistence(
        IDbContextFactory<HrManagementDbContext> dbContextFactory)
    {
        _dbContextFactory =
            dbContextFactory;
    }

    public async Task<bool> TryCreateAsync(
        UserAccount account,
        UserCredential credential,
        UserLoginSecurityState securityState,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(
            account);

        ArgumentNullException.ThrowIfNull(
            credential);

        ArgumentNullException.ThrowIfNull(
            securityState);

        if (account.Kind !=
            UserAccountKind.Owner)
        {
            throw new ArgumentException(
                "Tài khoản bootstrap phải là Owner.",
                nameof(account));
        }

        if (credential.AccountId !=
            account.Id)
        {
            throw new ArgumentException(
                "Credential không thuộc tài khoản bootstrap.",
                nameof(credential));
        }

        if (securityState.AccountId !=
            account.Id)
        {
            throw new ArgumentException(
                "Login security state không thuộc tài khoản bootstrap.",
                nameof(securityState));
        }

        await using HrManagementDbContext dbContext =
            await _dbContextFactory.CreateDbContextAsync(
                cancellationToken);

        var connection =
            (SqliteConnection)
                dbContext.Database.GetDbConnection();

        await connection.OpenAsync(
            cancellationToken);

        await using SqliteTransaction transaction =
            connection.BeginTransaction(
                deferred: false);

        await dbContext.Database.UseTransactionAsync(
            transaction,
            cancellationToken);

        bool alreadyBootstrapped =
            await dbContext.UserAccounts
                .AsNoTracking()
                .AnyAsync(
                    cancellationToken);

        if (alreadyBootstrapped)
        {
            await transaction.RollbackAsync(
                cancellationToken);

            return false;
        }

        await dbContext.UserAccounts.AddAsync(
            account,
            cancellationToken);

        await dbContext.UserCredentials.AddAsync(
            credential,
            cancellationToken);

        await dbContext.UserLoginSecurityStates.AddAsync(
            securityState,
            cancellationToken);

        await dbContext.SaveChangesAsync(
            cancellationToken);

        await transaction.CommitAsync(
            cancellationToken);

        return true;
    }
}
