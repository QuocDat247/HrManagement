using HrManagement.Application.Authentication.Accounts;
using HrManagement.Domain.Authentication.Accounts;
using HrManagement.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace HrManagement.Infrastructure.Authentication.Accounts;

public sealed class EfAccountProfileUpdatePersistence
    : IAccountProfileUpdatePersistence
{
    private readonly IDbContextFactory<HrManagementDbContext>
        _dbContextFactory;

    public EfAccountProfileUpdatePersistence(
        IDbContextFactory<HrManagementDbContext> dbContextFactory)
    {
        _dbContextFactory =
            dbContextFactory;
    }

    public async Task<AccountProfileUpdatePersistenceResult>
        TryUpdateAsync(
            Guid accountId,
            string displayName,
            Guid? employeeId,
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

            return AccountProfileUpdatePersistenceResult
                .AccountNotFound;
        }

        if (employeeId.HasValue)
        {
            Guid linkedEmployeeId =
                employeeId.Value;

            bool employeeExists =
                await dbContext
                    .Employees
                    .AsNoTracking()
                    .AnyAsync(
                        employee =>
                            employee.Id ==
                            linkedEmployeeId,
                        cancellationToken);

            if (!employeeExists)
            {
                await transaction.RollbackAsync(
                    cancellationToken);

                return AccountProfileUpdatePersistenceResult
                    .EmployeeNotFound;
            }

            bool employeeAlreadyLinked =
                await dbContext
                    .UserAccounts
                    .AsNoTracking()
                    .AnyAsync(
                        existing =>
                            existing.Id !=
                                accountId
                            && existing.EmployeeId ==
                                linkedEmployeeId,
                        cancellationToken);

            if (employeeAlreadyLinked)
            {
                await transaction.RollbackAsync(
                    cancellationToken);

                return AccountProfileUpdatePersistenceResult
                    .EmployeeAlreadyLinked;
            }
        }

        account.UpdateProfile(
            displayName,
            employeeId);

        await dbContext.SaveChangesAsync(
            cancellationToken);

        await transaction.CommitAsync(
            cancellationToken);

        return AccountProfileUpdatePersistenceResult
            .Updated;
    }
}
