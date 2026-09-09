using HrManagement.Application.Authentication.Accounts;
using HrManagement.Domain.Authentication.Accounts;
using HrManagement.Domain.Authentication.Credentials;
using HrManagement.Domain.Authentication.Security;
using HrManagement.Infrastructure.Authentication.Accounts;
using HrManagement.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace HrManagement.Tests.Persistence;

public sealed class EfStandardAccountCreationPersistenceTests
{
    [Fact]
    public async Task
        TryCreateAsync_WithValidAccount_PersistsWholeSecurityAggregate()
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
            new EfStandardAccountCreationPersistence(
                new TestDbContextFactory(
                    options));

        var account =
            new UserAccount(
                Guid.NewGuid(),
                "manager",
                "Quản lý",
                UserAccountKind.Standard);

        var credential =
            new UserCredential(
                account.Id,
                "$test$secure$hash",
                mustChangePassword:
                    true);

        var securityState =
            new UserLoginSecurityState(
                account.Id);

        StandardAccountCreationPersistenceResult result =
            await persistence.TryCreateAsync(
                account,
                credential,
                securityState);

        Assert.Equal(
            StandardAccountCreationPersistenceResult.Created,
            result);

        await using var verificationContext =
            new HrManagementDbContext(
                options);

        Assert.NotNull(
            await verificationContext
                .UserAccounts
                .AsNoTracking()
                .SingleOrDefaultAsync(
                    item =>
                        item.Id ==
                        account.Id));

        Assert.NotNull(
            await verificationContext
                .UserCredentials
                .AsNoTracking()
                .SingleOrDefaultAsync(
                    item =>
                        item.AccountId ==
                        account.Id));

        Assert.NotNull(
            await verificationContext
                .UserLoginSecurityStates
                .AsNoTracking()
                .SingleOrDefaultAsync(
                    item =>
                        item.AccountId ==
                        account.Id));
    }

    [Fact]
    public async Task
        TryCreateAsync_WithDuplicateUsername_DoesNotPersistNewAccount()
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

        Guid existingId =
            Guid.NewGuid();

        await using (var dbContext =
                     new HrManagementDbContext(
                         options))
        {
            await dbContext.Database
                .EnsureCreatedAsync();

            dbContext.UserAccounts.Add(
                new UserAccount(
                    existingId,
                    "manager",
                    "Existing",
                    UserAccountKind.Standard));

            await dbContext.SaveChangesAsync();
        }

        var persistence =
            new EfStandardAccountCreationPersistence(
                new TestDbContextFactory(
                    options));

        var account =
            new UserAccount(
                Guid.NewGuid(),
                "MANAGER",
                "New Manager",
                UserAccountKind.Standard);

        StandardAccountCreationPersistenceResult result =
            await persistence.TryCreateAsync(
                account,
                new UserCredential(
                    account.Id,
                    "$test$another$hash",
                    true),
                new UserLoginSecurityState(
                    account.Id));

        Assert.Equal(
            StandardAccountCreationPersistenceResult
                .UsernameAlreadyExists,
            result);

        await using var verificationContext =
            new HrManagementDbContext(
                options);

        Assert.Equal(
            1,
            await verificationContext
                .UserAccounts
                .CountAsync());

        Assert.False(
            await verificationContext
                .UserCredentials
                .AnyAsync(
                    item =>
                        item.AccountId ==
                        account.Id));
    }

    private sealed class TestDbContextFactory
        : IDbContextFactory<HrManagementDbContext>
    {
        private readonly DbContextOptions<HrManagementDbContext>
            _options;

        public TestDbContextFactory(
            DbContextOptions<HrManagementDbContext> options)
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
