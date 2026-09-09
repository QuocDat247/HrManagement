using HrManagement.Application.Authentication.Accounts;
using HrManagement.Domain.Authentication.Accounts;
using HrManagement.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace HrManagement.Infrastructure.Authentication.Accounts;

public sealed class EfAccountActiveStatePersistence
    : IAccountActiveStatePersistence
{
    private readonly IDbContextFactory<HrManagementDbContext>
        _dbContextFactory;

    public EfAccountActiveStatePersistence(
        IDbContextFactory<HrManagementDbContext> dbContextFactory)
    {
        _dbContextFactory =
            dbContextFactory;
    }

    public async Task<AccountActiveStatePersistenceResult>
        TrySetAsync(
            Guid accountId,
            bool isActive,
            CancellationToken cancellationToken = default)
    {
        if (accountId == Guid.Empty)
        {
            throw new ArgumentException(
                "Mã tài khoản không hợp lệ.",
                nameof(accountId));
        }

        await using HrManagementDbContext dbContext =
            await _dbContextFactory
                .CreateDbContextAsync(
                    cancellationToken);

        var connection =
            (SqliteConnection)
                dbContext.Database
                    .GetDbConnection();

        await connection.OpenAsync(
            cancellationToken);

        await using SqliteTransaction transaction =
            connection.BeginTransaction(
                deferred:
                    false);

        await dbContext.Database
            .UseTransactionAsync(
                transaction,
                cancellationToken);

        UserAccount? account =
            await dbContext
                .UserAccounts
                .SingleOrDefaultAsync(
                    item =>
                        item.Id ==
                        accountId,
                    cancellationToken);

        if (account is null)
        {
            await transaction.RollbackAsync(
                cancellationToken);

            return AccountActiveStatePersistenceResult
                .AccountNotFound;
        }

        if (account.IsActive ==
            isActive)
        {
            await transaction.RollbackAsync(
                cancellationToken);

            return AccountActiveStatePersistenceResult
                .Unchanged;
        }

        if (!isActive
            && account.Kind ==
                UserAccountKind.Owner)
        {
            bool hasOtherActiveOwner =
                await dbContext
                    .UserAccounts
                    .AsNoTracking()
                    .AnyAsync(
                        existing =>
                            existing.Id !=
                                accountId
                            && existing.Kind ==
                                UserAccountKind.Owner
                            && existing.IsActive,
                        cancellationToken);

            if (!hasOtherActiveOwner)
            {
                await transaction.RollbackAsync(
                    cancellationToken);

                return AccountActiveStatePersistenceResult
                    .LastActiveOwner;
            }
        }

        account.SetActive(
            isActive);

        await dbContext.SaveChangesAsync(
            cancellationToken);

        await transaction.CommitAsync(
            cancellationToken);

        return AccountActiveStatePersistenceResult
            .Updated;
    }
}
