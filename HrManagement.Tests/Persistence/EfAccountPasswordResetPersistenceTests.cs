using HrManagement.Domain.Authentication.Accounts;
using HrManagement.Domain.Authentication.Credentials;
using HrManagement.Domain.Authentication.Security;
using HrManagement.Infrastructure.Authentication.Credentials;
using HrManagement.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace HrManagement.Tests.Persistence;

public sealed class EfAccountPasswordResetPersistenceTests
{
    [Fact]
    public async Task
        TryResetAsync_UpdatesCredentialAndClearsLoginSecurityState()
    {
        await using var connection =
            new SqliteConnection(
                "Data Source=:memory:");

        await connection.OpenAsync();

        var options =
            new DbContextOptionsBuilder<
                    HrManagementDbContext>()
                .UseSqlite(
                    connection)
                .Options;

        Guid accountId =
            Guid.NewGuid();

        await using (var dbContext =
                     new HrManagementDbContext(
                         options))
        {
            await dbContext.Database
                .EnsureCreatedAsync();

            dbContext.UserAccounts.Add(
                new UserAccount(
                    accountId,
                    "employee",
                    "Nhân viên",
                    UserAccountKind.Standard));

            dbContext.UserCredentials.Add(
                new UserCredential(
                    accountId,
                    "$test$old$hash",
                    mustChangePassword:
                        false));

            dbContext.UserLoginSecurityStates.Add(
                new UserLoginSecurityState(
                    accountId,
                    failedLoginCount:
                        5,
                    lockoutEndUtc:
                        DateTimeOffset.UtcNow
                            .AddMinutes(
                                15)));

            await dbContext.SaveChangesAsync();
        }

        var persistence =
            new EfAccountPasswordResetPersistence(
                new TestDbContextFactory(
                    options));

        bool result =
            await persistence.TryResetAsync(
                accountId,
                "$test$new$hash");

        Assert.True(
            result);

        await using var verificationContext =
            new HrManagementDbContext(
                options);

        UserCredential credential =
            await verificationContext
                .UserCredentials
                .AsNoTracking()
                .SingleAsync(
                    item =>
                        item.AccountId ==
                        accountId);

        Assert.Equal(
            "$test$new$hash",
            credential.PasswordHash);

        Assert.True(
            credential.MustChangePassword);

        UserLoginSecurityState securityState =
            await verificationContext
                .UserLoginSecurityStates
                .AsNoTracking()
                .SingleAsync(
                    item =>
                        item.AccountId ==
                        accountId);

        Assert.Equal(
            0,
            securityState.FailedLoginCount);

        Assert.Null(
            securityState.LockoutEndUtc);
    }

    [Fact]
    public async Task
        TryResetAsync_WhenCredentialDoesNotExist_ReturnsFalse()
    {
        await using var connection =
            new SqliteConnection(
                "Data Source=:memory:");

        await connection.OpenAsync();

        var options =
            new DbContextOptionsBuilder<
                    HrManagementDbContext>()
                .UseSqlite(
                    connection)
                .Options;

        await using (var dbContext =
                     new HrManagementDbContext(
                         options))
        {
            await dbContext.Database
                .EnsureCreatedAsync();
        }

        var persistence =
            new EfAccountPasswordResetPersistence(
                new TestDbContextFactory(
                    options));

        bool result =
            await persistence.TryResetAsync(
                Guid.NewGuid(),
                "$test$new$hash");

        Assert.False(
            result);
    }

    private sealed class TestDbContextFactory
        : IDbContextFactory<HrManagementDbContext>
    {
        private readonly DbContextOptions<
            HrManagementDbContext>
            _options;

        public TestDbContextFactory(
            DbContextOptions<
                HrManagementDbContext> options)
        {
            _options =
                options;
        }

        public HrManagementDbContext CreateDbContext()
        {
            return new HrManagementDbContext(
                _options);
        }

        public Task<HrManagementDbContext>
            CreateDbContextAsync(
                CancellationToken cancellationToken = default)
        {
            return Task.FromResult(
                new HrManagementDbContext(
                    _options));
        }
    }
}
