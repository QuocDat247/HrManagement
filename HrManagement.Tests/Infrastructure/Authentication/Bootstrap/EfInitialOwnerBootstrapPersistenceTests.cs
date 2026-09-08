using HrManagement.Domain.Authentication.Accounts;
using HrManagement.Domain.Authentication.Credentials;
using HrManagement.Domain.Authentication.Security;
using HrManagement.Infrastructure.Authentication.Bootstrap;
using HrManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HrManagement.Tests.Infrastructure.Authentication.Bootstrap;

public sealed class
    EfInitialOwnerBootstrapPersistenceTests
{
    [Fact]
    public async Task
        TryCreateAsync_OnEmptyDatabase_CreatesCompleteBootstrapSet()
    {
        string databasePath =
            CreateDatabasePath();

        try
        {
            var factory =
                new TestDbContextFactory(
                    databasePath);

            await MigrateAsync(
                factory);

            var persistence =
                new EfInitialOwnerBootstrapPersistence(
                    factory);

            var bootstrapSet =
                CreateBootstrapSet(
                    "owner");

            bool created =
                await persistence.TryCreateAsync(
                    bootstrapSet.Account,
                    bootstrapSet.Credential,
                    bootstrapSet.SecurityState);

            Assert.True(
                created);

            await using HrManagementDbContext dbContext =
                await factory.CreateDbContextAsync();

            UserAccount account =
                await dbContext.UserAccounts
                    .AsNoTracking()
                    .SingleAsync();

            UserCredential credential =
                await dbContext.UserCredentials
                    .AsNoTracking()
                    .SingleAsync();

            UserLoginSecurityState securityState =
                await dbContext.UserLoginSecurityStates
                    .AsNoTracking()
                    .SingleAsync();

            Assert.Equal(
                bootstrapSet.Account.Id,
                account.Id);

            Assert.Equal(
                UserAccountKind.Owner,
                account.Kind);

            Assert.Equal(
                account.Id,
                credential.AccountId);

            Assert.Equal(
                "$test$owner$hash",
                credential.PasswordHash);

            Assert.False(
                credential.MustChangePassword);

            Assert.Equal(
                account.Id,
                securityState.AccountId);

            Assert.Equal(
                0,
                securityState.FailedLoginCount);

            Assert.Null(
                securityState.LockoutEndUtc);
        }
        finally
        {
            DeleteDatabase(
                databasePath);
        }
    }

    [Fact]
    public async Task
        TryCreateAsync_WhenAlreadyBootstrapped_ReturnsFalse()
    {
        string databasePath =
            CreateDatabasePath();

        try
        {
            var factory =
                new TestDbContextFactory(
                    databasePath);

            await MigrateAsync(
                factory);

            var persistence =
                new EfInitialOwnerBootstrapPersistence(
                    factory);

            var first =
                CreateBootstrapSet(
                    "owner-one");

            var second =
                CreateBootstrapSet(
                    "owner-two");

            bool firstCreated =
                await persistence.TryCreateAsync(
                    first.Account,
                    first.Credential,
                    first.SecurityState);

            bool secondCreated =
                await persistence.TryCreateAsync(
                    second.Account,
                    second.Credential,
                    second.SecurityState);

            Assert.True(
                firstCreated);

            Assert.False(
                secondCreated);

            await using HrManagementDbContext dbContext =
                await factory.CreateDbContextAsync();

            Assert.Equal(
                1,
                await dbContext.UserAccounts
                    .CountAsync());

            Assert.Equal(
                1,
                await dbContext.UserCredentials
                    .CountAsync());

            Assert.Equal(
                1,
                await dbContext.UserLoginSecurityStates
                    .CountAsync());
        }
        finally
        {
            DeleteDatabase(
                databasePath);
        }
    }

    [Fact]
    public async Task
        TryCreateAsync_ConcurrentCalls_CreatesOnlyOneOwner()
    {
        string databasePath =
            CreateDatabasePath();

        try
        {
            var factory =
                new TestDbContextFactory(
                    databasePath);

            await MigrateAsync(
                factory);

            var firstPersistence =
                new EfInitialOwnerBootstrapPersistence(
                    factory);

            var secondPersistence =
                new EfInitialOwnerBootstrapPersistence(
                    factory);

            var first =
                CreateBootstrapSet(
                    "owner-one");

            var second =
                CreateBootstrapSet(
                    "owner-two");

            Task<bool> firstTask =
                firstPersistence.TryCreateAsync(
                    first.Account,
                    first.Credential,
                    first.SecurityState);

            Task<bool> secondTask =
                secondPersistence.TryCreateAsync(
                    second.Account,
                    second.Credential,
                    second.SecurityState);

            bool[] results =
                await Task.WhenAll(
                    firstTask,
                    secondTask);

            Assert.Single(
                results.Where(
                    created =>
                        created));

            await using HrManagementDbContext dbContext =
                await factory.CreateDbContextAsync();

            Assert.Equal(
                1,
                await dbContext.UserAccounts
                    .CountAsync());

            Assert.Equal(
                1,
                await dbContext.UserCredentials
                    .CountAsync());

            Assert.Equal(
                1,
                await dbContext.UserLoginSecurityStates
                    .CountAsync());
        }
        finally
        {
            DeleteDatabase(
                databasePath);
        }
    }

    [Fact]
    public async Task
        TryCreateAsync_WithMismatchedCredential_Throws()
    {
        string databasePath =
            CreateDatabasePath();

        try
        {
            var factory =
                new TestDbContextFactory(
                    databasePath);

            await MigrateAsync(
                factory);

            var persistence =
                new EfInitialOwnerBootstrapPersistence(
                    factory);

            var bootstrapSet =
                CreateBootstrapSet(
                    "owner");

            var wrongCredential =
                new UserCredential(
                    Guid.NewGuid(),
                    "$test$wrong$hash",
                    mustChangePassword:
                        false);

            await Assert.ThrowsAsync<
                ArgumentException>(
                    () =>
                        persistence.TryCreateAsync(
                            bootstrapSet.Account,
                            wrongCredential,
                            bootstrapSet.SecurityState));
        }
        finally
        {
            DeleteDatabase(
                databasePath);
        }
    }

    private static (
        UserAccount Account,
        UserCredential Credential,
        UserLoginSecurityState SecurityState)
        CreateBootstrapSet(
            string username)
    {
        Guid accountId =
            Guid.NewGuid();

        var account =
            new UserAccount(
                accountId,
                username,
                "Chủ doanh nghiệp",
                UserAccountKind.Owner);

        var credential =
            new UserCredential(
                accountId,
                "$test$owner$hash",
                mustChangePassword:
                    false);

        var securityState =
            new UserLoginSecurityState(
                accountId);

        return (
            account,
            credential,
            securityState);
    }

    private static async Task MigrateAsync(
        IDbContextFactory<HrManagementDbContext> factory)
    {
        await using HrManagementDbContext dbContext =
            await factory.CreateDbContextAsync();

        await dbContext.Database
            .MigrateAsync();
    }

    private static string CreateDatabasePath()
    {
        return Path.Combine(
            Path.GetTempPath(),
            $"hrmanagement-owner-bootstrap-{Guid.NewGuid():N}.db");
    }

    private static void DeleteDatabase(
        string databasePath)
    {
        foreach (string path in new[]
        {
            databasePath,
            $"{databasePath}-shm",
            $"{databasePath}-wal"
        })
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }

    private sealed class TestDbContextFactory
        : IDbContextFactory<HrManagementDbContext>
    {
        private readonly DbContextOptions<HrManagementDbContext>
            _options;

        public TestDbContextFactory(
            string databasePath)
        {
            _options =
                new DbContextOptionsBuilder<HrManagementDbContext>()
                    .UseSqlite(
                        $"Data Source={databasePath};"
                        + "Pooling=False;"
                        + "Default Timeout=30")
                    .Options;
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
            cancellationToken.ThrowIfCancellationRequested();

            return Task.FromResult(
                CreateDbContext());
        }
    }
}
