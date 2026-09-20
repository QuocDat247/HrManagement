using HrManagement.Application.Authentication;
using HrManagement.Application.Authentication.Accounts;
using HrManagement.Application.Persistence.Backups;
using HrManagement.Domain.Authentication.Accounts;

namespace HrManagement.Tests.Application.Persistence.Backups;

public sealed class
    OwnerDatabaseMaintenanceServiceTests
{
    [Fact]
    public async Task
        CanManageAsync_WithActiveOwner_ReturnsTrue()
    {
        UserAccount owner =
            CreateAccount(
                UserAccountKind.Owner);

        var service =
            CreateService(
                owner,
                out _,
                out _);

        bool result =
            await service.CanManageAsync();

        Assert.True(
            result);
    }

    [Fact]
    public async Task
        CreateBackupAsync_WithStandardAccount_IsDeniedBeforeBackupRuns()
    {
        UserAccount standard =
            CreateAccount(
                UserAccountKind.Standard);

        var service =
            CreateService(
                standard,
                out FakeDatabaseBackupService backupService,
                out _);

        await Assert.ThrowsAsync<
            UnauthorizedAccessException>(
                () =>
                    service.CreateBackupAsync(
                        "blocked.hrbackup"));

        Assert.Equal(
            0,
            backupService.CallCount);
    }

    [Fact]
    public async Task
        RestoreAsync_WithInactiveOwner_IsDeniedBeforeRestoreRuns()
    {
        UserAccount owner =
            CreateAccount(
                UserAccountKind.Owner);

        owner.SetActive(
            false);

        var service =
            CreateService(
                owner,
                out _,
                out FakeDatabaseRestoreService restoreService);

        await Assert.ThrowsAsync<
            UnauthorizedAccessException>(
                () =>
                    service.RestoreAsync(
                        "blocked.hrbackup"));

        Assert.Equal(
            0,
            restoreService.CallCount);
    }

    [Fact]
    public async Task
        Owner_CanDelegateBackupAndRestore()
    {
        UserAccount owner =
            CreateAccount(
                UserAccountKind.Owner);

        var service =
            CreateService(
                owner,
                out FakeDatabaseBackupService backupService,
                out FakeDatabaseRestoreService restoreService);

        DatabaseBackupResult backupResult =
            await service.CreateBackupAsync(
                "owner.hrbackup");

        DatabaseRestoreResult restoreResult =
            await service.RestoreAsync(
                "owner.hrbackup");

        Assert.Equal(
            1,
            backupService.CallCount);

        Assert.Equal(
            1,
            restoreService.CallCount);

        Assert.Equal(
            "owner.hrbackup",
            backupResult.BackupFilePath);

        Assert.True(
            restoreResult.IsSuccessful);
    }

    private static OwnerDatabaseMaintenanceService
        CreateService(
            UserAccount account,
            out FakeDatabaseBackupService backupService,
            out FakeDatabaseRestoreService restoreService)
    {
        var currentUserContext =
            new FakeCurrentUserContext(
                new AuthenticatedUser(
                    account.Id.ToString(),
                    account.Username,
                    account.DisplayName));

        var accountRepository =
            new FakeUserAccountRepository(
                account);

        backupService =
            new FakeDatabaseBackupService();

        restoreService =
            new FakeDatabaseRestoreService();

        return new OwnerDatabaseMaintenanceService(
            currentUserContext,
            accountRepository,
            backupService,
            restoreService);
    }

    private static UserAccount CreateAccount(
        UserAccountKind kind)
    {
        return new UserAccount(
            Guid.NewGuid(),
            kind == UserAccountKind.Owner
                ? "owner"
                : "standard",
            kind == UserAccountKind.Owner
                ? "Owner"
                : "Standard",
            kind);
    }

    private sealed class FakeCurrentUserContext
        : ICurrentUserContext
    {
        public FakeCurrentUserContext(
            AuthenticatedUser? currentUser)
        {
            CurrentUser =
                currentUser;
        }

        public AuthenticatedUser? CurrentUser
        {
            get;
        }

        public bool IsAuthenticated =>
            CurrentUser is not null;
    }

    private sealed class FakeUserAccountRepository
        : IUserAccountRepository
    {
        private readonly UserAccount
            _account;

        public FakeUserAccountRepository(
            UserAccount account)
        {
            _account =
                account;
        }

        public Task<UserAccount?> GetByIdAsync(
            Guid id,
            CancellationToken cancellationToken = default)
        {
            cancellationToken
                .ThrowIfCancellationRequested();

            UserAccount? result =
                id == _account.Id
                    ? _account
                    : null;

            return Task.FromResult(
                result);
        }

        public Task<UserAccount?>
            GetByUsernameAsync(
                string username,
                CancellationToken cancellationToken = default)
        {
            cancellationToken
                .ThrowIfCancellationRequested();

            return Task.FromResult<
                UserAccount?>(
                    null);
        }

        public Task<IReadOnlyList<UserAccount>>
            GetAllAsync(
                CancellationToken cancellationToken = default)
        {
            cancellationToken
                .ThrowIfCancellationRequested();

            IReadOnlyList<UserAccount> result =
                new[]
                {
                    _account
                };

            return Task.FromResult(
                result);
        }

        public Task AddAsync(
            UserAccount account,
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task UpdateAsync(
            UserAccount account,
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }
    }

    private sealed class FakeDatabaseBackupService
        : IDatabaseBackupService
    {
        public int CallCount
        {
            get;
            private set;
        }

        public Task<DatabaseBackupResult>
            CreateAsync(
                string destinationFilePath,
                CancellationToken cancellationToken = default)
        {
            cancellationToken
                .ThrowIfCancellationRequested();

            CallCount++;

            return Task.FromResult(
                new DatabaseBackupResult(
                    destinationFilePath,
                    DateTimeOffset.UtcNow,
                    null));
        }
    }

    private sealed class FakeDatabaseRestoreService
        : IDatabaseRestoreService
    {
        public int CallCount
        {
            get;
            private set;
        }

        public Task<DatabaseRestoreResult>
            RestoreAsync(
                string backupFilePath,
                CancellationToken cancellationToken = default)
        {
            cancellationToken
                .ThrowIfCancellationRequested();

            CallCount++;

            return Task.FromResult(
                new DatabaseRestoreResult(
                    true));
        }
    }
}
