using HrManagement.Application.Authentication.Accounts;
using HrManagement.Application.Authentication.Bootstrap;
using HrManagement.Application.Authentication.Credentials;
using HrManagement.Domain.Authentication.Accounts;
using HrManagement.Domain.Authentication.Credentials;
using HrManagement.Domain.Authentication.Security;

namespace HrManagement.Tests.Authentication;

public sealed class InitialOwnerBootstrapServiceTests
{
    [Fact]
    public async Task
        IsBootstrapRequiredAsync_WithNoAccounts_ReturnsTrue()
    {
        var service =
            CreateService();

        bool result =
            await service
                .IsBootstrapRequiredAsync();

        Assert.True(
            result);
    }

    [Fact]
    public async Task
        IsBootstrapRequiredAsync_WithExistingAccount_ReturnsFalse()
    {
        UserAccount existingAccount =
            CreateExistingAccount();

        var service =
            CreateService(
                existingAccount);

        bool result =
            await service
                .IsBootstrapRequiredAsync();

        Assert.False(
            result);
    }

    [Fact]
    public async Task
        CreateInitialOwnerAsync_WithValidValues_CreatesOwner()
    {
        var persistence =
            new TestBootstrapPersistence();

        var service =
            CreateService(
                bootstrapPersistence:
                    persistence);

        OwnerBootstrapResult result =
            await service
                .CreateInitialOwnerAsync(
                    "owner",
                    "Chủ doanh nghiệp",
                    "A secure owner password 2026");

        Assert.True(
            result.IsSuccessful);

        Assert.Null(
            result.ErrorMessage);

        Assert.NotNull(
            persistence.Account);

        Assert.Equal(
            UserAccountKind.Owner,
            persistence.Account!.Kind);

        Assert.Equal(
            "owner",
            persistence.Account.Username);

        Assert.Null(
            persistence.Account.EmployeeId);

        Assert.NotNull(
            persistence.Credential);

        Assert.Equal(
            persistence.Account.Id,
            persistence.Credential!.AccountId);

        Assert.Equal(
            "$test$secure$hash",
            persistence.Credential.PasswordHash);

        Assert.False(
            persistence.Credential.MustChangePassword);

        Assert.NotNull(
            persistence.SecurityState);

        Assert.Equal(
            0,
            persistence.SecurityState!
                .FailedLoginCount);

        Assert.Null(
            persistence.SecurityState
                .LockoutEndUtc);
    }

    [Fact]
    public async Task
        CreateInitialOwnerAsync_WithRejectedPassword_DoesNotPersist()
    {
        var persistence =
            new TestBootstrapPersistence();

        var service =
            CreateService(
                bootstrapPersistence:
                    persistence);

        OwnerBootstrapResult result =
            await service
                .CreateInitialOwnerAsync(
                    "owner",
                    "Chủ doanh nghiệp",
                    "too-short");

        Assert.False(
            result.IsSuccessful);

        Assert.Null(
            persistence.Account);
    }

    [Fact]
    public async Task
        CreateInitialOwnerAsync_WhenAccountAlreadyExists_DoesNotPersist()
    {
        var persistence =
            new TestBootstrapPersistence();

        var service =
            CreateService(
                CreateExistingAccount(),
                persistence);

        OwnerBootstrapResult result =
            await service
                .CreateInitialOwnerAsync(
                    "owner",
                    "Chủ doanh nghiệp",
                    "A secure owner password 2026");

        Assert.False(
            result.IsSuccessful);

        Assert.Null(
            persistence.Account);
    }

    [Fact]
    public async Task
        CreateInitialOwnerAsync_WhenAtomicCreateLosesRace_ReturnsFailure()
    {
        var persistence =
            new TestBootstrapPersistence(
                allowCreate: false);

        var service =
            CreateService(
                bootstrapPersistence:
                    persistence);

        OwnerBootstrapResult result =
            await service
                .CreateInitialOwnerAsync(
                    "owner",
                    "Chủ doanh nghiệp",
                    "A secure owner password 2026");

        Assert.False(
            result.IsSuccessful);

        Assert.False(
            string.IsNullOrWhiteSpace(
                result.ErrorMessage));
    }

    private static InitialOwnerBootstrapService
        CreateService(
            UserAccount? existingAccount = null,
            TestBootstrapPersistence? bootstrapPersistence = null)
    {
        return new InitialOwnerBootstrapService(
            new TestUserAccountRepository(
                existingAccount),
            new DefaultPasswordPolicy(
                new TestPasswordBlocklist()),
            new TestPasswordHasher(),
            bootstrapPersistence
                ?? new TestBootstrapPersistence());
    }

    private static UserAccount
        CreateExistingAccount()
    {
        return new UserAccount(
            Guid.NewGuid(),
            "existing-user",
            "Người dùng hiện có",
            UserAccountKind.Standard);
    }

    private sealed class TestUserAccountRepository
        : IUserAccountRepository
    {
        private readonly UserAccount?
            _account;

        public TestUserAccountRepository(
            UserAccount? account)
        {
            _account =
                account;
        }

        public Task<IReadOnlyList<UserAccount>>
            GetAllAsync(
                CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            IReadOnlyList<UserAccount> result =
                _account is null
                    ? Array.Empty<UserAccount>()
                    : new[]
                    {
                        _account
                    };

            return Task.FromResult(
                result);
        }

        public Task<UserAccount?> GetByIdAsync(
            Guid id,
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task<UserAccount?> GetByUsernameAsync(
            string username,
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
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

    private sealed class TestPasswordHasher
        : IPasswordHasher
    {
        public string HashPassword(
            string password)
        {
            return "$test$secure$hash";
        }

        public PasswordVerificationResult VerifyPassword(
            string password,
            string passwordHash)
        {
            throw new NotSupportedException();
        }
    }

    private sealed class TestPasswordBlocklist
        : IPasswordBlocklist
    {
        public bool IsBlocked(
            string normalizedPassword)
        {
            return false;
        }
    }

    private sealed class TestBootstrapPersistence
        : IInitialOwnerBootstrapPersistence
    {
        private readonly bool
            _allowCreate;

        public TestBootstrapPersistence(
            bool allowCreate = true)
        {
            _allowCreate =
                allowCreate;
        }

        public UserAccount? Account
        {
            get;
            private set;
        }

        public UserCredential? Credential
        {
            get;
            private set;
        }

        public UserLoginSecurityState? SecurityState
        {
            get;
            private set;
        }

        public Task<bool> TryCreateAsync(
            UserAccount account,
            UserCredential credential,
            UserLoginSecurityState securityState,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!_allowCreate)
            {
                return Task.FromResult(
                    false);
            }

            Account =
                account;

            Credential =
                credential;

            SecurityState =
                securityState;

            return Task.FromResult(
                true);
        }
    }
}
