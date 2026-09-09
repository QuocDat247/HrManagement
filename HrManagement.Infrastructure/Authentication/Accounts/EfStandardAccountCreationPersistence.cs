using HrManagement.Application.Authentication.Accounts;
using HrManagement.Domain.Authentication.Accounts;
using HrManagement.Domain.Authentication.Credentials;
using HrManagement.Domain.Authentication.Security;
using HrManagement.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace HrManagement.Infrastructure.Authentication.Accounts;

public sealed class EfStandardAccountCreationPersistence
    : IStandardAccountCreationPersistence
{
    private readonly IDbContextFactory<HrManagementDbContext>
        _dbContextFactory;

    public EfStandardAccountCreationPersistence(
        IDbContextFactory<HrManagementDbContext> dbContextFactory)
    {
        _dbContextFactory =
            dbContextFactory;
    }

    public async Task<StandardAccountCreationPersistenceResult>
        TryCreateAsync(
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
            UserAccountKind.Standard)
        {
            throw new ArgumentException(
                "Account creation persistence chỉ chấp nhận tài khoản Standard.",
                nameof(account));
        }

        if (credential.AccountId !=
            account.Id)
        {
            throw new ArgumentException(
                "Credential không thuộc tài khoản.",
                nameof(credential));
        }

        if (!credential.MustChangePassword)
        {
            throw new ArgumentException(
                "Tài khoản mới phải đổi mật khẩu khi đăng nhập.",
                nameof(credential));
        }

        if (securityState.AccountId !=
            account.Id)
        {
            throw new ArgumentException(
                "Login security state không thuộc tài khoản.",
                nameof(securityState));
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

        string normalizedUsername =
            account.NormalizedUsername;

        bool usernameExists =
            await dbContext
                .UserAccounts
                .AsNoTracking()
                .AnyAsync(
                    existing =>
                        existing.NormalizedUsername ==
                        normalizedUsername,
                    cancellationToken);

        if (usernameExists)
        {
            await transaction.RollbackAsync(
                cancellationToken);

            return StandardAccountCreationPersistenceResult
                .UsernameAlreadyExists;
        }

        if (account.EmployeeId.HasValue)
        {
            Guid employeeId =
                account.EmployeeId.Value;

            bool employeeExists =
                await dbContext
                    .Employees
                    .AsNoTracking()
                    .AnyAsync(
                        employee =>
                            employee.Id ==
                            employeeId,
                        cancellationToken);

            if (!employeeExists)
            {
                await transaction.RollbackAsync(
                    cancellationToken);

                return StandardAccountCreationPersistenceResult
                    .EmployeeNotFound;
            }

            bool employeeAlreadyLinked =
                await dbContext
                    .UserAccounts
                    .AsNoTracking()
                    .AnyAsync(
                        existing =>
                            existing.EmployeeId ==
                            employeeId,
                        cancellationToken);

            if (employeeAlreadyLinked)
            {
                await transaction.RollbackAsync(
                    cancellationToken);

                return StandardAccountCreationPersistenceResult
                    .EmployeeAlreadyLinked;
            }
        }

        await dbContext.UserAccounts
            .AddAsync(
                account,
                cancellationToken);

        await dbContext.UserCredentials
            .AddAsync(
                credential,
                cancellationToken);

        await dbContext.UserLoginSecurityStates
            .AddAsync(
                securityState,
                cancellationToken);

        await dbContext.SaveChangesAsync(
            cancellationToken);

        await transaction.CommitAsync(
            cancellationToken);

        return StandardAccountCreationPersistenceResult
            .Created;
    }
}
