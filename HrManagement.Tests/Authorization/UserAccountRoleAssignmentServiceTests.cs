using HrManagement.Application.Authentication;
using HrManagement.Application.Authorization;
using HrManagement.Application.Authorization.Roles;
using HrManagement.Domain.Authorization.Permissions;
using HrManagement.Domain.Authorization.Roles;

namespace HrManagement.Tests.Authorization;

public sealed class UserAccountRoleAssignmentServiceTests
{
    [Fact]
    public async Task
        ReplaceAsync_DeduplicatesRoleIds()
    {
        Guid actorId =
            Guid.NewGuid();

        Guid accountId =
            Guid.NewGuid();

        Guid firstRoleId =
            Guid.NewGuid();

        Guid secondRoleId =
            Guid.NewGuid();

        var persistence =
            new TestPersistence();

        var service =
            new UserAccountRoleAssignmentService(
                new TestCurrentUserContext(
                    actorId),
                persistence);

        AccountRoleAssignmentResult result =
            await service.ReplaceAsync(
                new ReplaceAccountRolesRequest(
                    accountId,
                    new[]
                    {
                        firstRoleId,
                        firstRoleId,
                        secondRoleId
                    }));

        Assert.True(
            result.IsSuccessful);

        Assert.Equal(
            actorId,
            persistence.ActorAccountId);

        Assert.Equal(
            accountId,
            persistence.AccountId);

        Assert.Equal(
            2,
            persistence.Assignments.Count);
    }

    [Fact]
    public async Task
        ReplaceAsync_WithEmptyRoleId_DoesNotPersist()
    {
        var persistence =
            new TestPersistence();

        var service =
            new UserAccountRoleAssignmentService(
                new TestCurrentUserContext(
                    Guid.NewGuid()),
                persistence);

        AccountRoleAssignmentResult result =
            await service.ReplaceAsync(
                new ReplaceAccountRolesRequest(
                    Guid.NewGuid(),
                    new[]
                    {
                        Guid.Empty
                    }));

        Assert.False(
            result.IsSuccessful);

        Assert.False(
            persistence.Called);
    }

    [Fact]
    public async Task
        ReplaceAsync_WhenPersistenceDetectsEscalation_ReturnsFailure()
    {
        var persistence =
            new TestPersistence
            {
                Result =
                    UserAccountRoleAssignmentPersistenceResult
                        .PermissionEscalation
            };

        var service =
            new UserAccountRoleAssignmentService(
                new TestCurrentUserContext(
                    Guid.NewGuid()),
                persistence);

        AccountRoleAssignmentResult result =
            await service.ReplaceAsync(
                new ReplaceAccountRolesRequest(
                    Guid.NewGuid(),
                    new[]
                    {
                        Guid.NewGuid()
                    }));

        Assert.False(
            result.IsSuccessful);

        Assert.False(
            string.IsNullOrWhiteSpace(
                result.ErrorMessage));
    }

    [Fact]
    public async Task
        AuthorizedService_RequiresAccountAssignRole()
    {
        var guard =
            new TestAuthorizationGuard();

        var inner =
            new TestAssignmentService();

        var service =
            new AuthorizedUserAccountRoleAssignmentService(
                inner,
                guard);

        await service.ReplaceAsync(
            new ReplaceAccountRolesRequest(
                Guid.NewGuid(),
                Array.Empty<Guid>()));

        Assert.Equal(
            PermissionCodes.AccountAssignRole,
            guard.LastPermissionCode);

        Assert.True(
            inner.Called);
    }

    [Fact]
    public async Task
        AuthorizedService_WhenDenied_DoesNotInvokeInner()
    {
        var inner =
            new TestAssignmentService();

        var service =
            new AuthorizedUserAccountRoleAssignmentService(
                inner,
                new TestAuthorizationGuard(
                    deny:
                        true));

        await Assert.ThrowsAsync<
            AuthorizationDeniedException>(
                () =>
                    service.ReplaceAsync(
                        new ReplaceAccountRolesRequest(
                            Guid.NewGuid(),
                            Array.Empty<Guid>())));

        Assert.False(
            inner.Called);
    }

    private sealed class TestCurrentUserContext
        : ICurrentUserContext
    {
        public TestCurrentUserContext(
            Guid accountId)
        {
            CurrentUser =
                new AuthenticatedUser(
                    accountId.ToString(
                        "D"),
                    "test-user",
                    "Test User");
        }

        public AuthenticatedUser? CurrentUser
        {
            get;
        }

        public bool IsAuthenticated =>
            CurrentUser is not null;
    }

    private sealed class TestPersistence
        : IUserAccountRoleAssignmentPersistence
    {
        public UserAccountRoleAssignmentPersistenceResult Result
        {
            get;
            set;
        } =
            UserAccountRoleAssignmentPersistenceResult.Updated;

        public bool Called
        {
            get;
            private set;
        }

        public Guid ActorAccountId
        {
            get;
            private set;
        }

        public Guid AccountId
        {
            get;
            private set;
        }

        public IReadOnlyCollection<UserAccountRole> Assignments
        {
            get;
            private set;
        } =
            Array.Empty<UserAccountRole>();

        public Task<UserAccountRoleAssignmentPersistenceResult>
            TryReplaceAsync(
                Guid actorAccountId,
                Guid accountId,
                IReadOnlyCollection<UserAccountRole> assignments,
                CancellationToken cancellationToken = default)
        {
            Called =
                true;

            ActorAccountId =
                actorAccountId;

            AccountId =
                accountId;

            Assignments =
                assignments;

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

    private sealed class TestAssignmentService
        : IUserAccountRoleAssignmentService
    {
        public bool Called
        {
            get;
            private set;
        }

        public Task<AccountRoleAssignmentResult> ReplaceAsync(
            ReplaceAccountRolesRequest request,
            CancellationToken cancellationToken = default)
        {
            Called =
                true;

            return Task.FromResult(
                new AccountRoleAssignmentResult(
                    true,
                    request.AccountId));
        }
    }
}
