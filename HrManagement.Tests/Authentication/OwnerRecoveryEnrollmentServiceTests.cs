using HrManagement.Application.Authentication;
using HrManagement.Application.Authentication.Accounts;
using HrManagement.Application.Authentication.Recovery;
using HrManagement.Domain.Authentication.Accounts;
using HrManagement.Domain.Authentication.Recovery;

namespace HrManagement.Tests.Authentication;

public sealed class OwnerRecoveryEnrollmentServiceTests
{
    [Fact]
    public async Task
        IsEnrollmentRequiredAsync_WhenOwnerHasNoRecoveryCredential_ReturnsTrue()
    {
        UserAccount owner =
            CreateAccount(
                UserAccountKind.Owner);

        var persistence =
            new TestPersistence();

        var service =
            CreateService(
                owner,
                persistence);

        bool result =
            await service
                .IsEnrollmentRequiredAsync();

        Assert.True(
            result);
    }

    [Fact]
    public async Task
        IsEnrollmentRequiredAsync_WhenOwnerAlreadyEnrolled_ReturnsFalse()
    {
        UserAccount owner =
            CreateAccount(
                UserAccountKind.Owner);

        var persistence =
            new TestPersistence
            {
                Exists =
                    true
            };

        var service =
            CreateService(
                owner,
                persistence);

        bool result =
            await service
                .IsEnrollmentRequiredAsync();

        Assert.False(
            result);
    }

    [Fact]
    public async Task
        IsEnrollmentRequiredAsync_WhenCurrentAccountIsStandard_ReturnsFalse()
    {
        UserAccount standard =
            CreateAccount(
                UserAccountKind.Standard);

        var service =
            CreateService(
                standard,
                new TestPersistence());

        bool result =
            await service
                .IsEnrollmentRequiredAsync();

        Assert.False(
            result);
    }

    [Fact]
    public async Task
        EnrollAsync_WhenOwnerAndCodeValid_PersistsHashedCredential()
    {
        UserAccount owner =
            CreateAccount(
                UserAccountKind.Owner);

        var persistence =
            new TestPersistence();

        var service =
            CreateService(
                owner,
                persistence);

        OwnerRecoveryEnrollmentResult result =
            await service.EnrollAsync(
                "7F3A-91C8-2D6E-B447-A120-8F9C-35D2-61EA");

        Assert.True(
            result.IsSuccessful);

        Assert.NotNull(
            persistence.CreatedCredential);

        Assert.Equal(
            owner.Id,
            persistence.CreatedCredential!
                .AccountId);

        Assert.Equal(
            "$test$recovery$hash",
            persistence.CreatedCredential
                .RecoveryCodeHash);
    }

    [Fact]
    public async Task
        EnrollAsync_WithInvalidCode_DoesNotPersist()
    {
        UserAccount owner =
            CreateAccount(
                UserAccountKind.Owner);

        var persistence =
            new TestPersistence();

        var service =
            CreateService(
                owner,
                persistence);

        OwnerRecoveryEnrollmentResult result =
            await service.EnrollAsync(
                "not-a-valid-code");

        Assert.False(
            result.IsSuccessful);

        Assert.Null(
            persistence.CreatedCredential);
    }

    [Fact]
    public async Task
        EnrollAsync_WhenCurrentAccountIsStandard_DoesNotPersist()
    {
        UserAccount standard =
            CreateAccount(
                UserAccountKind.Standard);

        var persistence =
            new TestPersistence();

        var service =
            CreateService(
                standard,
                persistence);

        OwnerRecoveryEnrollmentResult result =
            await service.EnrollAsync(
                "7F3A-91C8-2D6E-B447-A120-8F9C-35D2-61EA");

        Assert.False(
            result.IsSuccessful);

        Assert.Null(
            persistence.CreatedCredential);
    }

    [Fact]
    public async Task
        EnrollAsync_WhenCredentialAlreadyExists_DoesNotReplaceIt()
    {
        UserAccount owner =
            CreateAccount(
                UserAccountKind.Owner);

        var persistence =
            new TestPersistence
            {
                Exists =
                    true
            };

        var service =
            CreateService(
                owner,
                persistence);

        OwnerRecoveryEnrollmentResult result =
            await service.EnrollAsync(
                "7F3A-91C8-2D6E-B447-A120-8F9C-35D2-61EA");

        Assert.False(
            result.IsSuccessful);

        Assert.Null(
            persistence.CreatedCredential);
    }

    private static OwnerRecoveryEnrollmentService
        CreateService(
            UserAccount account,
            TestPersistence persistence)
    {
        return new OwnerRecoveryEnrollmentService(
            new TestCurrentUserContext(
                account),
            new TestUserAccountRepository(
                account),
            new TestRecoveryCodeGenerator(),
            new TestRecoveryCodeHasher(),
            persistence);
    }

    private static UserAccount CreateAccount(
        UserAccountKind kind)
    {
        return new UserAccount(
            Guid.NewGuid(),
            kind == UserAccountKind.Owner
                ? "owner"
                : "employee",
            kind == UserAccountKind.Owner
                ? "Chủ doanh nghiệp"
                : "Nhân viên",
            kind);
    }

    private sealed class TestCurrentUserContext
        : ICurrentUserContext
    {
        public TestCurrentUserContext(
            UserAccount account)
        {
            CurrentUser =
                new AuthenticatedUser(
                    account.Id.ToString("D"),
                    account.Username,
                    account.DisplayName);
        }

        public AuthenticatedUser? CurrentUser
        {
            get;
        }

        public bool IsAuthenticated =>
            CurrentUser is not null;
    }

    private sealed class TestUserAccountRepository
        : IUserAccountRepository
    {
        private readonly UserAccount
            _account;

        public TestUserAccountRepository(
            UserAccount account)
        {
            _account =
                account;
        }

        public Task<IReadOnlyList<UserAccount>>
            GetAllAsync(
                CancellationToken cancellationToken = default)
        {
            IReadOnlyList<UserAccount> result =
                new[]
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
            return Task.FromResult<UserAccount?>(
                id == _account.Id
                    ? _account
                    : null);
        }

        public Task<UserAccount?> GetByUsernameAsync(
            string username,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<UserAccount?>(
                string.Equals(
                    username,
                    _account.Username,
                    StringComparison.OrdinalIgnoreCase)
                    ? _account
                    : null);
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

    private sealed class TestRecoveryCodeGenerator
        : IRecoveryCodeGenerator
    {
        public string Generate()
        {
            return
                "7F3A-91C8-2D6E-B447-A120-8F9C-35D2-61EA";
        }
    }

    private sealed class TestRecoveryCodeHasher
        : IRecoveryCodeHasher
    {
        public string Hash(
            string recoveryCode)
        {
            return "$test$recovery$hash";
        }

        public bool Verify(
            string recoveryCode,
            string recoveryCodeHash)
        {
            return false;
        }
    }

    private sealed class TestPersistence
        : IOwnerRecoveryEnrollmentPersistence
    {
        public bool Exists
        {
            get;
            set;
        }

        public OwnerRecoveryCredential?
            CreatedCredential
        {
            get;
            private set;
        }

        public Task<bool> ExistsAsync(
            Guid accountId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(
                Exists);
        }

        public Task<bool> TryCreateAsync(
            OwnerRecoveryCredential credential,
            CancellationToken cancellationToken = default)
        {
            if (Exists)
            {
                return Task.FromResult(
                    false);
            }

            CreatedCredential =
                credential;

            Exists =
                true;

            return Task.FromResult(
                true);
        }
    }
}
