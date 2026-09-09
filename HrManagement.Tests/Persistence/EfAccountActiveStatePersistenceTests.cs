using HrManagement.Application.Authentication.Accounts;
using HrManagement.Domain.Authentication.Accounts;
using HrManagement.Infrastructure.Authentication.Accounts;
using HrManagement.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace HrManagement.Tests.Persistence;

public sealed class EfAccountActiveStatePersistenceTests
{
    [Fact]
    public async Task
        TrySetAsync_CanDisableStandardAccount()
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
                    "manager",
                    "Quản lý",
                    UserAccountKind.Standard));

            await dbContext.SaveChangesAsync();
        }

        var persistence =
            new EfAccountActiveStatePersistence(
                new TestDbContextFactory(
                    options));

        AccountActiveStatePersistenceResult result =
            await persistence.TrySetAsync(
                accountId,
                false);

        Assert.Equal(
            AccountActiveStatePersistenceResult.Updated,
            result);

        await using var verificationContext =
            new HrManagementDbContext(
                options);

        UserAccount saved =
            await verificationContext
                .UserAccounts
                .AsNoTracking()
                .SingleAsync(
                    account =>
                        account.Id ==
                        accountId);

        Assert.False(
            saved.IsActive);

        Assert.Equal(
            UserAccountKind.Standard,
            saved.Kind);

        Assert.Equal(
            "manager",
            saved.Username);

        Assert.Equal(
            "Quản lý",
            saved.DisplayName);
    }

    [Fact]
    public async Task
        TrySetAsync_CannotDisableLastActiveOwner()
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

        Guid ownerId =
            Guid.NewGuid();

        await using (var dbContext =
                     new HrManagementDbContext(
                         options))
        {
            await dbContext.Database
                .EnsureCreatedAsync();

            dbContext.UserAccounts.Add(
                new UserAccount(
                    ownerId,
                    "owner",
                    "Chủ doanh nghiệp",
                    UserAccountKind.Owner));

            await dbContext.SaveChangesAsync();
        }

        var persistence =
            new EfAccountActiveStatePersistence(
                new TestDbContextFactory(
                    options));

        AccountActiveStatePersistenceResult result =
            await persistence.TrySetAsync(
                ownerId,
                false);

        Assert.Equal(
            AccountActiveStatePersistenceResult
                .LastActiveOwner,
            result);

        await using var verificationContext =
            new HrManagementDbContext(
                options);

        UserAccount saved =
            await verificationContext
                .UserAccounts
                .AsNoTracking()
                .SingleAsync(
                    account =>
                        account.Id ==
                        ownerId);

        Assert.True(
            saved.IsActive);
    }

    [Fact]
    public async Task
        TrySetAsync_CanDisableOwnerWhenAnotherActiveOwnerExists()
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

        Guid firstOwnerId =
            Guid.NewGuid();

        Guid secondOwnerId =
            Guid.NewGuid();

        await using (var dbContext =
                     new HrManagementDbContext(
                         options))
        {
            await dbContext.Database
                .EnsureCreatedAsync();

            dbContext.UserAccounts.AddRange(
                new UserAccount(
                    firstOwnerId,
                    "owner.one",
                    "Owner One",
                    UserAccountKind.Owner),
                new UserAccount(
                    secondOwnerId,
                    "owner.two",
                    "Owner Two",
                    UserAccountKind.Owner));

            await dbContext.SaveChangesAsync();
        }

        var persistence =
            new EfAccountActiveStatePersistence(
                new TestDbContextFactory(
                    options));

        AccountActiveStatePersistenceResult result =
            await persistence.TrySetAsync(
                firstOwnerId,
                false);

        Assert.Equal(
            AccountActiveStatePersistenceResult.Updated,
            result);

        await using var verificationContext =
            new HrManagementDbContext(
                options);

        UserAccount firstOwner =
            await verificationContext
                .UserAccounts
                .AsNoTracking()
                .SingleAsync(
                    account =>
                        account.Id ==
                        firstOwnerId);

        UserAccount secondOwner =
            await verificationContext
                .UserAccounts
                .AsNoTracking()
                .SingleAsync(
                    account =>
                        account.Id ==
                        secondOwnerId);

        Assert.False(
            firstOwner.IsActive);

        Assert.True(
            secondOwner.IsActive);
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
