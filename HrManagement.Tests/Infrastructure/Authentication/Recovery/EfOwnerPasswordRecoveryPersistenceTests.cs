using HrManagement.Domain.Authentication.Accounts;
using HrManagement.Domain.Authentication.Credentials;
using HrManagement.Domain.Authentication.Recovery;
using HrManagement.Domain.Authentication.Security;
using HrManagement.Infrastructure.Authentication.Recovery;
using HrManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HrManagement.Tests.Infrastructure.Authentication.Recovery;

public sealed class EfOwnerPasswordRecoveryPersistenceTests
{
    [Fact]
    public async Task
        TryRecoverAsync_WithExpectedRecoveryHash_UpdatesWholeRecoverySet()
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

            Guid accountId =
                Guid.NewGuid();

            await SeedOwnerAsync(
                factory,
                accountId);

            var persistence =
                new EfOwnerPasswordRecoveryPersistence(
                    factory);

            bool recovered =
                await persistence.TryRecoverAsync(
                    accountId,
                    "$test$old$recovery",
                    "$test$new$password",
                    "$test$new$recovery");

            Assert.True(
                recovered);

            await using HrManagementDbContext dbContext =
                await factory.CreateDbContextAsync();

            UserCredential credential =
                await dbContext.UserCredentials
                    .AsNoTracking()
                    .SingleAsync(
                        item =>
                            item.AccountId ==
                            accountId);

            Assert.Equal(
                "$test$new$password",
                credential.PasswordHash);

            Assert.False(
                credential.MustChangePassword);

            OwnerRecoveryCredential recoveryCredential =
                await dbContext.OwnerRecoveryCredentials
                    .AsNoTracking()
                    .SingleAsync(
                        item =>
                            item.AccountId ==
                            accountId);

            Assert.Equal(
                "$test$new$recovery",
                recoveryCredential.RecoveryCodeHash);

            UserLoginSecurityState securityState =
                await dbContext.UserLoginSecurityStates
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
        finally
        {
            DeleteDatabase(
                databasePath);
        }
    }

    [Fact]
    public async Task
        TryRecoverAsync_WithStaleRecoveryHash_DoesNotChangeAnything()
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

            Guid accountId =
                Guid.NewGuid();

            await SeedOwnerAsync(
                factory,
                accountId);

            var persistence =
                new EfOwnerPasswordRecoveryPersistence(
                    factory);

            bool recovered =
                await persistence.TryRecoverAsync(
                    accountId,
                    "$test$stale$recovery",
                    "$test$new$password",
                    "$test$new$recovery");

            Assert.False(
                recovered);

            await using HrManagementDbContext dbContext =
                await factory.CreateDbContextAsync();

            UserCredential credential =
                await dbContext.UserCredentials
                    .AsNoTracking()
                    .SingleAsync();

            Assert.Equal(
                "$test$old$password",
                credential.PasswordHash);

            OwnerRecoveryCredential recoveryCredential =
                await dbContext.OwnerRecoveryCredentials
                    .AsNoTracking()
                    .SingleAsync();

            Assert.Equal(
                "$test$old$recovery",
                recoveryCredential.RecoveryCodeHash);

            UserLoginSecurityState securityState =
                await dbContext.UserLoginSecurityStates
                    .AsNoTracking()
                    .SingleAsync();

            Assert.Equal(
                5,
                securityState.FailedLoginCount);

            Assert.NotNull(
                securityState.LockoutEndUtc);
        }
        finally
        {
            DeleteDatabase(
                databasePath);
        }
    }

    private static async Task SeedOwnerAsync(
        IDbContextFactory<HrManagementDbContext> factory,
        Guid accountId)
    {
        await using HrManagementDbContext dbContext =
            await factory.CreateDbContextAsync();

        dbContext.UserAccounts.Add(
            new UserAccount(
                accountId,
                "owner",
                "Chủ doanh nghiệp",
                UserAccountKind.Owner));

        dbContext.UserCredentials.Add(
            new UserCredential(
                accountId,
                "$test$old$password",
                mustChangePassword:
                    false));

        dbContext.OwnerRecoveryCredentials.Add(
            new OwnerRecoveryCredential(
                accountId,
                "$test$old$recovery"));

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
            $"hrmanagement-owner-recovery-{Guid.NewGuid():N}.db");
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
        private readonly DbContextOptions<
            HrManagementDbContext>
            _options;

        public TestDbContextFactory(
            string databasePath)
        {
            _options =
                new DbContextOptionsBuilder<
                        HrManagementDbContext>()
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
            cancellationToken
                .ThrowIfCancellationRequested();

            return Task.FromResult(
                CreateDbContext());
        }
    }
}
