using HrManagement.Application.Authentication;
using HrManagement.Application.Authentication.Security;
using HrManagement.Domain.Authentication.Accounts;
using HrManagement.Domain.Authentication.Credentials;
using HrManagement.Domain.Authentication.Security;
using HrManagement.Infrastructure.Authentication;
using HrManagement.Infrastructure.Authentication.Accounts;
using HrManagement.Infrastructure.Authentication.Credentials;
using HrManagement.Infrastructure.Authentication.Security;
using HrManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HrManagement.Tests.Authentication;

public sealed class RealAuthenticationServiceLockoutTests
{
    private const string CorrectPassword =
        "A valid commercial password 2026";

    [Fact]
    public async Task
        LoginAsync_AfterFiveFailures_LocksAccountTemporarily()
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
                await SeedAccountAsync(
                    factory);

            var timeProvider =
                new TestTimeProvider(
                    new DateTimeOffset(
                        2026,
                        9,
                        7,
                        9,
                        0,
                        0,
                        TimeSpan.Zero));

            var securityRepository =
                new EfUserLoginSecurityStateRepository(
                    factory);

            var service =
                CreateService(
                    factory,
                    securityRepository,
                    timeProvider);

            for (int attempt = 0;
                 attempt < 5;
                 attempt++)
            {
                AuthenticationResult result =
                    await service.LoginAsync(
                        "owner",
                        "Wrong password value");

                Assert.False(
                    result.IsSuccessful);
            }

            var state =
                await securityRepository
                    .GetByAccountIdAsync(
                        accountId);

            Assert.NotNull(
                state);

            Assert.Equal(
                5,
                state.FailedLoginCount);

            Assert.Equal(
                timeProvider
                    .GetUtcNow()
                    .AddMinutes(15),
                state.LockoutEndUtc);

            AuthenticationResult lockedResult =
                await service.LoginAsync(
                    "owner",
                    CorrectPassword);

            Assert.False(
                lockedResult.IsSuccessful);
        }
        finally
        {
            DeleteDatabase(
                databasePath);
        }
    }

    [Fact]
    public async Task
        LoginAsync_AfterLockoutExpires_AllowsCorrectPassword()
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
                await SeedAccountAsync(
                    factory);

            var timeProvider =
                new TestTimeProvider(
                    new DateTimeOffset(
                        2026,
                        9,
                        7,
                        9,
                        0,
                        0,
                        TimeSpan.Zero));

            var securityRepository =
                new EfUserLoginSecurityStateRepository(
                    factory);

            var service =
                CreateService(
                    factory,
                    securityRepository,
                    timeProvider);

            for (int attempt = 0;
                 attempt < 5;
                 attempt++)
            {
                await service.LoginAsync(
                    "owner",
                    "Wrong password value");
            }

            timeProvider.Advance(
                TimeSpan.FromMinutes(
                    15));

            AuthenticationResult result =
                await service.LoginAsync(
                    "owner",
                    CorrectPassword);

            Assert.True(
                result.IsSuccessful);

            UserLoginSecurityState? state =
                await securityRepository
                    .GetByAccountIdAsync(
                        accountId);

            Assert.NotNull(
                state);

            Assert.Equal(
                0,
                state.FailedLoginCount);

            Assert.Null(
                state.LockoutEndUtc);
        }
        finally
        {
            DeleteDatabase(
                databasePath);
        }
    }

    [Fact]
    public async Task
        LoginAsync_AfterExpiredLockoutAndWrongPassword_RestartsCounter()
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
                await SeedAccountAsync(
                    factory);

            var timeProvider =
                new TestTimeProvider(
                    new DateTimeOffset(
                        2026,
                        9,
                        7,
                        9,
                        0,
                        0,
                        TimeSpan.Zero));

            var securityRepository =
                new EfUserLoginSecurityStateRepository(
                    factory);

            var service =
                CreateService(
                    factory,
                    securityRepository,
                    timeProvider);

            for (int attempt = 0;
                 attempt < 5;
                 attempt++)
            {
                await service.LoginAsync(
                    "owner",
                    "Wrong password value");
            }

            timeProvider.Advance(
                TimeSpan.FromMinutes(
                    15));

            await service.LoginAsync(
                "owner",
                "Wrong password again");

            UserLoginSecurityState? state =
                await securityRepository
                    .GetByAccountIdAsync(
                        accountId);

            Assert.NotNull(
                state);

            Assert.Equal(
                1,
                state.FailedLoginCount);

            Assert.Null(
                state.LockoutEndUtc);
        }
        finally
        {
            DeleteDatabase(
                databasePath);
        }
    }

    private static RealAuthenticationService
        CreateService(
            IDbContextFactory<HrManagementDbContext> factory,
            IUserLoginSecurityStateRepository securityRepository,
            TimeProvider timeProvider)
    {
        return new RealAuthenticationService(
            new EfUserAccountRepository(
                factory),
            new EfUserCredentialRepository(
                factory),
            securityRepository,
            new Pbkdf2PasswordHasher(),
            new CurrentUserSession(),
            timeProvider);
    }

    private static async Task<Guid> SeedAccountAsync(
        IDbContextFactory<HrManagementDbContext> factory)
    {
        Guid accountId =
            Guid.NewGuid();

        await new EfUserAccountRepository(
                factory)
            .AddAsync(
                new UserAccount(
                    accountId,
                    "owner",
                    "Chủ doanh nghiệp",
                    UserAccountKind.Owner));

        var hasher =
            new Pbkdf2PasswordHasher();

        await new EfUserCredentialRepository(
                factory)
            .AddAsync(
                new UserCredential(
                    accountId,
                    hasher.HashPassword(
                        CorrectPassword),
                    mustChangePassword:
                        false));

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
            $"hrmanagement-lockout-{Guid.NewGuid():N}.db");
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

    private sealed class TestTimeProvider
        : TimeProvider
    {
        private DateTimeOffset
            _utcNow;

        public TestTimeProvider(
            DateTimeOffset utcNow)
        {
            _utcNow =
                utcNow.ToUniversalTime();
        }

        public override DateTimeOffset GetUtcNow()
        {
            return _utcNow;
        }

        public void Advance(
            TimeSpan duration)
        {
            _utcNow =
                _utcNow.Add(
                    duration);
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
