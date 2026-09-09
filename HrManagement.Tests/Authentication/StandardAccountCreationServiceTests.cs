using HrManagement.Application.Authentication.Accounts;
using HrManagement.Application.Authentication.Credentials;
using HrManagement.Application.Authorization;
using HrManagement.Domain.Authentication.Accounts;
using HrManagement.Domain.Authentication.Credentials;
using HrManagement.Domain.Authentication.Security;
using HrManagement.Domain.Authorization.Permissions;

namespace HrManagement.Tests.Authentication;

public sealed class StandardAccountCreationServiceTests
{
    [Fact]
    public async Task
        CreateAsync_WithValidValues_CreatesStandardAccount()
    {
        var persistence =
            new TestPersistence();

        var service =
            CreateCoreService(
                persistence);

        Guid employeeId =
            Guid.NewGuid();

        CreateStandardAccountResult result =
            await service.CreateAsync(
                new CreateStandardAccountRequest(
                    "manager",
                    "Quản lý",
                    "A secure temporary password 2026",
                    employeeId));

        Assert.True(
            result.IsSuccessful);

        Assert.NotNull(
            result.AccountId);

        Assert.NotNull(
            persistence.Account);

        Assert.Equal(
            UserAccountKind.Standard,
            persistence.Account!.Kind);

        Assert.Equal(
            employeeId,
            persistence.Account.EmployeeId);

        Assert.NotNull(
            persistence.Credential);

        Assert.True(
            persistence.Credential!
                .MustChangePassword);

        Assert.Equal(
            "$test$secure$hash",
            persistence.Credential
                .PasswordHash);

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
        CreateAsync_WithRejectedPassword_DoesNotPersist()
    {
        var persistence =
            new TestPersistence();

        var service =
            new StandardAccountCreationService(
                new TestPasswordPolicy(
                    PasswordPolicyResult.Rejected(
                        PasswordPolicyViolation.TooShort)),
                new TestPasswordHasher(),
                persistence);

        CreateStandardAccountResult result =
            await service.CreateAsync(
                new CreateStandardAccountRequest(
                    "manager",
                    "Quản lý",
                    "short"));

        Assert.False(
            result.IsSuccessful);

        Assert.False(
            persistence.Called);
    }

    [Theory]
    [InlineData(
        StandardAccountCreationPersistenceResult.UsernameAlreadyExists)]
    [InlineData(
        StandardAccountCreationPersistenceResult.EmployeeNotFound)]
    [InlineData(
        StandardAccountCreationPersistenceResult.EmployeeAlreadyLinked)]
    public async Task
        CreateAsync_WhenPersistenceRejects_ReturnsFailure(
            StandardAccountCreationPersistenceResult persistenceResult)
    {
        var persistence =
            new TestPersistence
            {
                Result =
                    persistenceResult
            };

        var service =
            CreateCoreService(
                persistence);

        CreateStandardAccountResult result =
            await service.CreateAsync(
                new CreateStandardAccountRequest(
                    "manager",
                    "Quản lý",
                    "A secure temporary password 2026"));

        Assert.False(
            result.IsSuccessful);

        Assert.False(
            string.IsNullOrWhiteSpace(
                result.ErrorMessage));
    }

    [Fact]
    public async Task
        AuthorizedService_RequiresAccountCreate()
    {
        var guard =
            new TestAuthorizationGuard();

        var inner =
            new TestCreationService();

        var service =
            new AuthorizedStandardAccountCreationService(
                inner,
                guard);

        await service.CreateAsync(
            new CreateStandardAccountRequest(
                "manager",
                "Quản lý",
                "temporary-password"));

        Assert.Equal(
            PermissionCodes.AccountCreate,
            guard.LastPermissionCode);

        Assert.True(
            inner.Called);
    }

    [Fact]
    public async Task
        AuthorizedService_WhenDenied_DoesNotInvokeInner()
    {
        var inner =
            new TestCreationService();

        var service =
            new AuthorizedStandardAccountCreationService(
                inner,
                new TestAuthorizationGuard(
                    deny:
                        true));

        await Assert.ThrowsAsync<
            AuthorizationDeniedException>(
                () =>
                    service.CreateAsync(
                        new CreateStandardAccountRequest(
                            "manager",
                            "Quản lý",
                            "temporary-password")));

        Assert.False(
            inner.Called);
    }

    private static StandardAccountCreationService
        CreateCoreService(
            TestPersistence persistence)
    {
        return new StandardAccountCreationService(
            new TestPasswordPolicy(
                PasswordPolicyResult.Accepted()),
            new TestPasswordHasher(),
            persistence);
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
            return "$test$secure$hash";
        }

        public PasswordVerificationResult VerifyPassword(
            string password,
            string passwordHash)
        {
            return PasswordVerificationResult.Failed;
        }
    }

    private sealed class TestPersistence
        : IStandardAccountCreationPersistence
    {
        public StandardAccountCreationPersistenceResult Result
        {
            get;
            set;
        } =
            StandardAccountCreationPersistenceResult.Created;

        public bool Called
        {
            get;
            private set;
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

        public Task<StandardAccountCreationPersistenceResult>
            TryCreateAsync(
                UserAccount account,
                UserCredential credential,
                UserLoginSecurityState securityState,
                CancellationToken cancellationToken = default)
        {
            Called =
                true;

            Account =
                account;

            Credential =
                credential;

            SecurityState =
                securityState;

            return Task.FromResult(
                Result);
        }
    }

    private sealed class TestAuthorizationGuard
        : IAuthorizationGuard
    {
        private readonly bool
            _deny;

        public TestAuthorizationGuard(
            bool deny = false)
        {
            _deny =
                deny;
        }

        public string? LastPermissionCode
        {
            get;
            private set;
        }

        public Task RequirePermissionAsync(
            string permissionCode,
            CancellationToken cancellationToken = default)
        {
            LastPermissionCode =
                permissionCode;

            if (_deny)
            {
                throw new AuthorizationDeniedException(
                    permissionCode);
            }

            return Task.CompletedTask;
        }
    }

    private sealed class TestCreationService
        : IStandardAccountCreationService
    {
        public bool Called
        {
            get;
            private set;
        }

        public Task<CreateStandardAccountResult> CreateAsync(
            CreateStandardAccountRequest request,
            CancellationToken cancellationToken = default)
        {
            Called =
                true;

            return Task.FromResult(
                new CreateStandardAccountResult(
                    true,
                    Guid.NewGuid()));
        }
    }
}
