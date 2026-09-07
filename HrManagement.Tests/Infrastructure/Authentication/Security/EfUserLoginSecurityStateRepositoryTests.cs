using HrManagement.Domain.Authentication.Accounts;
using HrManagement.Domain.Authentication.Security;
using HrManagement.Infrastructure.Authentication.Accounts;
using HrManagement.Infrastructure.Authentication.Security;
using HrManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HrManagement.Tests.Infrastructure.Authentication.Security;

public sealed class EfUserLoginSecurityStateRepositoryTests
{
    [Fact]
    public async Task
        AddAsync_ThenGetByAccountId_ReturnsState()
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
                await AddAccountAsync(
                    factory);

            DateTimeOffset lockoutEnd =
                new DateTimeOffset(
                    2026,
                    9,
                    7,
                    10,
                    30,
                    0,
                    TimeSpan.Zero);

            var repository =
                new EfUserLoginSecurityStateRepository(
                    factory);

            await repository.AddAsync(
                new UserLoginSecurityState(
                    accountId,
                    failedLoginCount: 5,
                    lockoutEndUtc:
                        lockoutEnd));

            UserLoginSecurityState? loaded =
                await repository
                    .GetByAccountIdAsync(
                        accountId);

            Assert.NotNull(
                loaded);

            Assert.Equal(
                accountId,
                loaded.AccountId);

            Assert.Equal(
                5,
                loaded.FailedLoginCount);

            Assert.Equal(
                lockoutEnd,
                loaded.LockoutEndUtc);
        }
        finally
        {
            DeleteDatabase(
                databasePath);
        }
    }

    [Fact]
    public async Task
        UpdateAsync_ReplacesFailureState()
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
                await AddAccountAsync(
                    factory);

            var repository =
                new EfUserLoginSecurityStateRepository(
                    factory);

            await repository.AddAsync(
                new UserLoginSecurityState(
                    accountId,
                    failedLoginCount: 2));

            DateTimeOffset lockoutEnd =
                new DateTimeOffset(
                    2026,
                    9,
                    7,
                    11,
                    0,
                    0,
                    TimeSpan.Zero);

            await repository.UpdateAsync(
                new UserLoginSecurityState(
                    accountId,
                    failedLoginCount: 5,
                    lockoutEndUtc:
                        lockoutEnd));

            UserLoginSecurityState? loaded =
                await repository
                    .GetByAccountIdAsync(
                        accountId);

            Assert.NotNull(
                loaded);

            Assert.Equal(
                5,
                loaded.FailedLoginCount);

            Assert.Equal(
                lockoutEnd,
                loaded.LockoutEndUtc);
        }
        finally
        {
            DeleteDatabase(
                databasePath);
        }
    }

    [Fact]
    public async Task
        Migrations_CreateUserLoginSecurityStatesTable()
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

            List<UserLoginSecurityState> states =
                await dbContext
                    .UserLoginSecurityStates
                    .AsNoTracking()
                    .ToListAsync();

            Assert.Empty(
                states);
        }
        finally
        {
            DeleteDatabase(
                databasePath);
        }
    }

    private static async Task<Guid> AddAccountAsync(
        IDbContextFactory<HrManagementDbContext> factory)
    {
        Guid accountId =
            Guid.NewGuid();

        var repository =
            new EfUserAccountRepository(
                factory);

        await repository.AddAsync(
            new UserAccount(
                accountId,
                $"user-{accountId:N}",
                "Người dùng kiểm thử",
                UserAccountKind.Standard));

        return accountId;
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
            $"hrmanagement-login-security-{Guid.NewGuid():N}.db");
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
