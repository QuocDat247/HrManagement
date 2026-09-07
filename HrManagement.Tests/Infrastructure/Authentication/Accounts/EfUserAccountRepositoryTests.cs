using HrManagement.Domain.Authentication.Accounts;
using HrManagement.Infrastructure.Authentication.Accounts;
using HrManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HrManagement.Tests.Infrastructure.Authentication.Accounts;

public sealed class EfUserAccountRepositoryTests
{
    [Fact]
    public async Task
        AddAsync_ThenGetByUsername_ReturnsPersistedAccount()
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

            var repository =
                new EfUserAccountRepository(
                    factory);

            Guid accountId =
                Guid.NewGuid();

            var account =
                new UserAccount(
                    accountId,
                    "owner.admin",
                    "Chủ doanh nghiệp",
                    UserAccountKind.Owner);

            await repository.AddAsync(
                account);

            UserAccount? loaded =
                await repository.GetByUsernameAsync(
                    " OWNER.ADMIN ");

            Assert.NotNull(
                loaded);

            Assert.Equal(
                accountId,
                loaded.Id);

            Assert.Equal(
                "owner.admin",
                loaded.Username);

            Assert.Equal(
                "OWNER.ADMIN",
                loaded.NormalizedUsername);

            Assert.Equal(
                UserAccountKind.Owner,
                loaded.Kind);
        }
        finally
        {
            DeleteDatabase(
                databasePath);
        }
    }

    [Fact]
    public async Task
        AddAsync_WithDuplicateNormalizedUsername_Throws()
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

            var repository =
                new EfUserAccountRepository(
                    factory);

            await repository.AddAsync(
                new UserAccount(
                    Guid.NewGuid(),
                    "Admin",
                    "Admin A",
                    UserAccountKind.Standard));

            var duplicate =
                new UserAccount(
                    Guid.NewGuid(),
                    "admin",
                    "Admin B",
                    UserAccountKind.Standard);

            await Assert.ThrowsAsync<
                DbUpdateException>(
                () =>
                    repository.AddAsync(
                        duplicate));
        }
        finally
        {
            DeleteDatabase(
                databasePath);
        }
    }

    [Fact]
    public async Task
        Migrations_CreateUserAccountsTable()
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

            List<UserAccount> accounts =
                await dbContext.UserAccounts
                    .AsNoTracking()
                    .ToListAsync();

            Assert.Empty(
                accounts);
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
            $"hrmanagement-auth-{Guid.NewGuid():N}.db");
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
