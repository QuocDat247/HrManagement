using HrManagement.Application.Authentication;
using HrManagement.Application.Authentication.Accounts;
using HrManagement.Application.Authentication.Credentials;
using HrManagement.Domain.Authentication.Accounts;

namespace HrManagement.Tests.Authentication;

public sealed class AccountPasswordResetServiceTests
{
    [Fact]
    public async Task
        ResetAsync_WhenCurrentUserIsStandard_DoesNotResetPassword()
    {
        var currentAccount =
            new UserAccount(
                Guid.NewGuid(),
                "manager",
                "Quản lý",
                UserAccountKind.Standard);

        var targetAccount =
            new UserAccount(
                Guid.NewGuid(),
                "employee",
                "Nhân viên",
                UserAccountKind.Standard);

        var persistence =
            new TestPersistence();

        var service =
            CreateService(
                currentAccount,
                targetAccount,
                persistence);

        ResetAccountPasswordResult result =
            await service.ResetAsync(
                new ResetAccountPasswordRequest(
                    targetAccount.Id,
                    "A secure temporary password 2026"));

        Assert.False(
            result.IsSuccessful);

        Assert.False(
            persistence.Called);
    }

    [Fact]
    public async Task
        ResetAsync_WhenTargetIsOwner_DoesNotResetPassword()
    {
        var currentOwner =
            new UserAccount(
                Guid.NewGuid(),
                "owner",
                "Owner",
                UserAccountKind.Owner);

        var targetOwner =
            new UserAccount(
                Guid.NewGuid(),
                "owner2",
                "Owner khác",
                UserAccountKind.Owner);

        var persistence =
            new TestPersistence();

        var service =
            CreateService(
                currentOwner,
                targetOwner,
                persistence);

        ResetAccountPasswordResult result =
            await service.ResetAsync(
                new ResetAccountPasswordRequest(
                    targetOwner.Id,
                    "A secure temporary password 2026"));

        Assert.False(
            result.IsSuccessful);

        Assert.False(
            persistence.Called);
    }

    [Fact]
    public async Task
        ResetAsync_WhenOwnerResetsStandard_PersistsNewHash()
    {
        var currentOwner =
            new UserAccount(
                Guid.NewGuid(),
                "owner",
                "Owner",
                UserAccountKind.Owner);

        var targetAccount =
            new UserAccount(
                Guid.NewGuid(),
                "employee",
                "Nhân viên",
                UserAccountKind.Standard);

        var persistence =
            new TestPersistence();

        var service =
            CreateService(
                currentOwner,
                targetAccount,
                persistence);

        ResetAccountPasswordResult result =
            await service.ResetAsync(
                new ResetAccountPasswordRequest(
                    targetAccount.Id,
                    "A secure temporary password 2026"));

        Assert.True(
            result.IsSuccessful);

        Assert.True(
            persistence.Called);

        Assert.Equal(
            targetAccount.Id,
            persistence.AccountId);

        Assert.Equal(
            "$test$reset$hash",
            persistence.PasswordHash);
    }

    [Fact]
    public async Task
        ResetAsync_WhenPasswordPolicyRejects_DoesNotPersist()
    {
        var currentOwner =
            new UserAccount(
                Guid.NewGuid(),
                "owner",
                "Owner",
                UserAccountKind.Owner);

        var targetAccount =
            new UserAccount(
                Guid.NewGuid(),
                "employee",
                "Nhân viên",
                UserAccountKind.Standard);

        var persistence =
            new TestPersistence();

        var repository =
            new TestAccountRepository(
                currentOwner,
                targetAccount);

        var service =
            new AccountPasswordResetService(
                new TestCurrentUserContext(
                    currentOwner),
                repository,
                new TestPasswordPolicy(
                    PasswordPolicyResult.Rejected(
                        PasswordPolicyViolation.TooShort)),
                new TestPasswordHasher(),
                persistence);

        ResetAccountPasswordResult result =
            await service.ResetAsync(
                new ResetAccountPasswordRequest(
                    targetAccount.Id,
                    "short"));

        Assert.False(
            result.IsSuccessful);

        Assert.False(
            persistence.Called);
    }

    private static AccountPasswordResetService
        CreateService(
            UserAccount currentAccount,
            UserAccount targetAccount,
            TestPersistence persistence)
    {
        return new AccountPasswordResetService(
            new TestCurrentUserContext(
                currentAccount),
            new TestAccountRepository(
                currentAccount,
                targetAccount),
            new TestPasswordPolicy(
                PasswordPolicyResult.Accepted()),
            new TestPasswordHasher(),
            persistence);
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

    private sealed class TestAccountRepository
        : IUserAccountRepository
    {
        private readonly Dictionary<Guid, UserAccount>
            _accounts;

        public TestAccountRepository(
            params UserAccount[] accounts)
        {
            _accounts =
                accounts.ToDictionary(
                    account =>
                        account.Id);
        }

        public Task<IReadOnlyList<UserAccount>>
            GetAllAsync(
                CancellationToken cancellationToken = default)
        {
            return Task.FromResult<
                IReadOnlyList<UserAccount>>(
                    _accounts.Values.ToArray());
        }

        public Task<UserAccount?> GetByIdAsync(
            Guid id,
            CancellationToken cancellationToken = default)
        {
            _accounts.TryGetValue(
                id,
                out UserAccount? account);

            return Task.FromResult(
                account);
        }

        public Task<UserAccount?> GetByUsernameAsync(
            string username,
            CancellationToken cancellationToken = default)
        {
            UserAccount? account =
                _accounts.Values
                    .FirstOrDefault(
                        item =>
                            string.Equals(
                                item.Username,
                                username,
                                StringComparison.OrdinalIgnoreCase));

            return Task.FromResult(
                account);
        }

        public Task AddAsync(
            UserAccount account,
            CancellationToken cancellationToken = default)
        {
            _accounts[account.Id] =
                account;

            return Task.CompletedTask;
        }

        public Task UpdateAsync(
            UserAccount account,
            CancellationToken cancellationToken = default)
        {
            _accounts[account.Id] =
                account;

            return Task.CompletedTask;
        }
    }

    private sealed class TestPasswordPolicy
        : IPasswordPolicy
    {
        private readonly PasswordPolicyResult
            _result;

        public TestPasswordPolicy(
            PasswordPolicyResult result)
        {
            _result =
                result;
        }

        public PasswordPolicyResult Evaluate(
            string password,
            string? username = null)
        {
            return _result;
        }
    }

    private sealed class TestPasswordHasher
        : IPasswordHasher
    {
        public string HashPassword(
            string password)
        {
            return "$test$reset$hash";
        }

        public PasswordVerificationResult VerifyPassword(
            string password,
            string passwordHash)
        {
            return PasswordVerificationResult.Failed;
        }
    }

    private sealed class TestPersistence
        : IAccountPasswordResetPersistence
    {
        public bool Called
        {
            get;
            private set;
        }

        public Guid AccountId
        {
            get;
            private set;
        }

        public string? PasswordHash
        {
            get;
            private set;
        }

        public bool Result
        {
            get;
            set;
        } =
            true;

        public Task<bool> TryResetAsync(
            Guid accountId,
            string passwordHash,
            CancellationToken cancellationToken = default)
        {
            Called =
                true;

            AccountId =
                accountId;

            PasswordHash =
                passwordHash;

            return Task.FromResult(
                Result);
        }
    }
}
