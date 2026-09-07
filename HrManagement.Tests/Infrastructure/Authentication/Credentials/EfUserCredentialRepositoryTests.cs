using HrManagement.Domain.Authentication.Accounts;
using HrManagement.Domain.Authentication.Credentials;
using HrManagement.Infrastructure.Authentication.Accounts;
using HrManagement.Infrastructure.Authentication.Credentials;
using HrManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HrManagement.Tests.Infrastructure.Authentication.Credentials;

public sealed class EfUserCredentialRepositoryTests
{
    [Fact]
    public async Task
        AddAsync_ThenGetByAccountId_ReturnsCredential()
    {
        string databasePath =
            CreateDatabasePath();

        try
        {
            var factory =
                new TestDbContextFactory(
                    databasePath);

            await EnsureCreatedAsync(
                factory);

            Guid accountId =
                Guid.NewGuid();

            var accountRepository =
                new EfUserAccountRepository(
                    factory);

            await accountRepository.AddAsync(
                new UserAccount(
                    accountId,
                    "owner",
                    "Chủ doanh nghiệp",
                    UserAccountKind.Owner));

            var credentialRepository =
                new EfUserCredentialRepository(
                    factory);

            const string passwordHash =
                "$example$opaque$hash";

            await credentialRepository.AddAsync(
                new UserCredential(
                    accountId,
                    passwordHash));

            UserCredential? loaded =
                await credentialRepository
                    .GetByAccountIdAsync(
                        accountId);

            Assert.NotNull(
                loaded);

            Assert.Equal(
                accountId,
                loaded.AccountId);

            Assert.Equal(
                passwordHash,
                loaded.PasswordHash);

            Assert.True(
                loaded.MustChangePassword);
        }
        finally
        {
            DeleteDatabase(
                databasePath);
        }
    }

    [Fact]
    public async Task
        UpdateAsync_ReplacesPasswordHashAndState()
    {
        string databasePath =
            CreateDatabasePath();

        try
        {
            var factory =
                new TestDbContextFactory(
                    databasePath);

            await EnsureCreatedAsync(
                factory);

            Guid accountId =
                Guid.NewGuid();

            var accountRepository =
                new EfUserAccountRepository(
                    factory);

            await accountRepository.AddAsync(
                new UserAccount(
                    accountId,
                    "accounting",
                    "Kế toán",
                    UserAccountKind.Standard));

            var credentialRepository =
                new EfUserCredentialRepository(
                    factory);

            await credentialRepository.AddAsync(
                new UserCredential(
                    accountId,
                    "$old$opaque$hash"));

            await credentialRepository.UpdateAsync(
                new UserCredential(
                    accountId,
                    "$new$opaque$hash",
                    mustChangePassword: false));

            UserCredential? loaded =
                await credentialRepository
                    .GetByAccountIdAsync(
                        accountId);

            Assert.NotNull(
                loaded);

            Assert.Equal(
                "$new$opaque$hash",
                loaded.PasswordHash);

            Assert.False(
                loaded.MustChangePassword);
        }
        finally
        {
            DeleteDatabase(
                databasePath);
        }
    }

    [Fact]
    public async Task
        Migrations_CreateUserCredentialsTable()
    {
        string databasePath =
            CreateDatabasePath();

        try
        {
            var factory =
                new TestDbContextFactory(
                    databasePath);

            await using HrManagementDbContext dbContext =
                await factory.CreateDbContextAsync();

            await dbContext.Database
                .MigrateAsync();

            List<UserCredential> credentials =
                await dbContext.UserCredentials
                    .AsNoTracking()
                    .ToListAsync();

            Assert.Empty(
                credentials);
        }
        finally
        {
            DeleteDatabase(
                databasePath);
        }
    }

    private static async Task EnsureCreatedAsync(
        IDbContextFactory<HrManagementDbContext> factory)
    {
        await using HrManagementDbContext dbContext =
            await factory.CreateDbContextAsync();

        await dbContext.Database
            .EnsureCreatedAsync();
    }

    private static string CreateDatabasePath()
    {
        return Path.Combine(
            Path.GetTempPath(),
            $"hrmanagement-credential-{Guid.NewGuid():N}.db");
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
                        $"Data Source={databasePath};Pooling=False")
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
