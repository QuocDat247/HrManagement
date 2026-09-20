using HrManagement.Application.Authentication.Accounts;
using HrManagement.Application.Authentication.Credentials;
using HrManagement.Application.Authentication.Recovery;
using HrManagement.Domain.Authentication.Accounts;

namespace HrManagement.Tests.Authentication;

public sealed class OwnerPasswordRecoveryServiceTests
{
    [Fact]
    public async Task
        RecoverAsync_WithValidOwnerAndRecoveryCode_ReturnsRotatedCode()
    {
        UserAccount owner =
            CreateAccount(
                UserAccountKind.Owner);

        var persistence =
            new TestPersistence
            {
                RecoveryCodeHash =
                    "$test$old$recovery"
            };

        var service =
            CreateService(
                owner,
                persistence);

        OwnerPasswordRecoveryResult result =
            await service.RecoverAsync(
                new OwnerPasswordRecoveryRequest(
                    owner.Username,
                    "7F3A-91C8-2D6E-B447-A120-8F9C-35D2-61EA",
                    "A new secure owner password 2026"));

        Assert.True(
            result.IsSuccessful);

        Assert.Equal(
            "1111-2222-3333-4444-5555-6666-7777-8888",
            result.NewRecoveryCode);

        Assert.True(
            persistence.RecoverCalled);

        Assert.Equal(
            owner.Id,
            persistence.AccountId);

        Assert.Equal(
            "$test$new$password",
            persistence.NewPasswordHash);

        Assert.Equal(
            "$test$new$recovery",
            persistence.NewRecoveryCodeHash);
    }

    [Fact]
    public async Task
        RecoverAsync_WithWrongRecoveryCode_DoesNotPersist()
    {
        UserAccount owner =
            CreateAccount(
                UserAccountKind.Owner);

        var persistence =
            new TestPersistence
            {
                RecoveryCodeHash =
                    "$test$old$recovery"
            };

        var service =
            CreateService(
                owner,
                persistence,
                recoveryCodeMatches:
                    false);

        OwnerPasswordRecoveryResult result =
            await service.RecoverAsync(
                new OwnerPasswordRecoveryRequest(
                    owner.Username,
                    "7F3A-91C8-2D6E-B447-A120-8F9C-35D2-61EA",
                    "A new secure owner password 2026"));

        Assert.False(
            result.IsSuccessful);

        Assert.False(
            persistence.RecoverCalled);
    }

    [Fact]
    public async Task
        RecoverAsync_WhenAccountIsStandard_DoesNotPersist()
    {
        UserAccount standard =
            CreateAccount(
                UserAccountKind.Standard);

        var persistence =
            new TestPersistence
            {
                RecoveryCodeHash =
                    "$test$old$recovery"
            };

        var service =
            CreateService(
                standard,
                persistence);

        OwnerPasswordRecoveryResult result =
            await service.RecoverAsync(
                new OwnerPasswordRecoveryRequest(
                    standard.Username,
                    "7F3A-91C8-2D6E-B447-A120-8F9C-35D2-61EA",
                    "A new secure owner password 2026"));

        Assert.False(
            result.IsSuccessful);

        Assert.False(
            persistence.RecoverCalled);
    }

    [Fact]
    public async Task
        RecoverAsync_WhenPasswordPolicyRejects_DoesNotPersist()
    {
        UserAccount owner =
            CreateAccount(
                UserAccountKind.Owner);

        var persistence =
            new TestPersistence();

        var service =
            new OwnerPasswordRecoveryService(
                new TestUserAccountRepository(
                    owner),
                new TestPasswordPolicy(
                    PasswordPolicyResult.Rejected(
                        PasswordPolicyViolation.TooShort)),
                new TestPasswordHasher(),
                new TestRecoveryCodeHasher(
                    true),
                new TestRecoveryCodeGenerator(),
                persistence);

        OwnerPasswordRecoveryResult result =
            await service.RecoverAsync(
                new OwnerPasswordRecoveryRequest(
                    owner.Username,
                    "7F3A-91C8-2D6E-B447-A120-8F9C-35D2-61EA",
                    "short"));

        Assert.False(
            result.IsSuccessful);

        Assert.False(
            persistence.RecoverCalled);
    }

    [Fact]
    public async Task
        RecoverAsync_WhenExpectedRecoveryHashChanged_ReturnsFailure()
    {
        UserAccount owner =
            CreateAccount(
                UserAccountKind.Owner);

        var persistence =
            new TestPersistence
            {
                RecoveryCodeHash =
                    "$test$old$recovery",

                RecoverResult =
                    false
            };

        var service =
            CreateService(
                owner,
                persistence);

        OwnerPasswordRecoveryResult result =
            await service.RecoverAsync(
                new OwnerPasswordRecoveryRequest(
                    owner.Username,
                    "7F3A-91C8-2D6E-B447-A120-8F9C-35D2-61EA",
                    "A new secure owner password 2026"));

        Assert.False(
            result.IsSuccessful);

        Assert.True(
            persistence.RecoverCalled);

        Assert.Null(
            result.NewRecoveryCode);
    }

    private static OwnerPasswordRecoveryService
        CreateService(
            UserAccount account,
            TestPersistence persistence,
            bool recoveryCodeMatches = true)
    {
        return new OwnerPasswordRecoveryService(
            new TestUserAccountRepository(
                account),
            new TestPasswordPolicy(
                PasswordPolicyResult.Accepted()),
            new TestPasswordHasher(),
            new TestRecoveryCodeHasher(
                recoveryCodeMatches),
            new TestRecoveryCodeGenerator(),
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
            return "$test$new$password";
        }

        public PasswordVerificationResult VerifyPassword(
            string password,
            string passwordHash)
        {
            return PasswordVerificationResult.Failed;
        }
    }

    private sealed class TestRecoveryCodeHasher
        : IRecoveryCodeHasher
    {
        private readonly bool
            _matches;

        public TestRecoveryCodeHasher(
            bool matches)
        {
            _matches =
                matches;
        }

        public string Hash(
            string recoveryCode)
        {
            return "$test$new$recovery";
        }

        public bool Verify(
            string recoveryCode,
            string recoveryCodeHash)
        {
            return _matches;
        }
    }

    private sealed class TestRecoveryCodeGenerator
        : IRecoveryCodeGenerator
    {
        public string Generate()
        {
            return
                "1111-2222-3333-4444-5555-6666-7777-8888";
        }
    }

    private sealed class TestPersistence
        : IOwnerPasswordRecoveryPersistence
    {
        public string? RecoveryCodeHash
        {
            get;
            set;
        }

        public bool RecoverResult
        {
            get;
            set;
        } =
            true;

        public bool RecoverCalled
        {
            get;
            private set;
        }

        public Guid AccountId
        {
            get;
            private set;
        }

        public string? NewPasswordHash
        {
            get;
            private set;
        }

        public string? NewRecoveryCodeHash
        {
            get;
            private set;
        }

        public Task<string?> GetRecoveryCodeHashAsync(
            Guid accountId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(
                RecoveryCodeHash);
        }

        public Task<bool> TryRecoverAsync(
            Guid accountId,
            string expectedRecoveryCodeHash,
            string newPasswordHash,
            string newRecoveryCodeHash,
            CancellationToken cancellationToken = default)
        {
            RecoverCalled =
                true;

            AccountId =
                accountId;

            NewPasswordHash =
                newPasswordHash;

            NewRecoveryCodeHash =
                newRecoveryCodeHash;

            return Task.FromResult(
                RecoverResult);
        }
    }
}
