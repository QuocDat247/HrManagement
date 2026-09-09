using HrManagement.Application.Authentication;
using HrManagement.Application.Authorization;
using HrManagement.Application.Authorization.Roles;
using HrManagement.Domain.Authorization.Permissions;
using HrManagement.Domain.Authorization.Roles;

namespace HrManagement.Tests.Authorization;

public sealed class RolePermissionAssignmentServiceTests
{
    [Fact]
    public async Task
        ReplaceAsync_NormalizesAndDeduplicatesPermissions()
    {
        Guid actorId =
            Guid.NewGuid();

        Guid roleId =
            Guid.NewGuid();

        var persistence =
            new TestPersistence();

        var service =
            new RolePermissionAssignmentService(
                new TestCurrentUserContext(
                    actorId),
                persistence);

        RoleManagementResult result =
            await service.ReplaceAsync(
                roleId,
                new[]
                {
                    PermissionCodes.EmployeeView,
                    $"  {PermissionCodes.EmployeeView}  ",
                    PermissionCodes.EmployeeEdit
                });

        Assert.True(
            result.IsSuccessful);

        Assert.Equal(
            actorId,
            persistence.ActorAccountId);

        Assert.Equal(
            roleId,
            persistence.RoleId);

        Assert.Equal(
            2,
            persistence.Permissions.Count);

        Assert.Contains(
            persistence.Permissions,
            permission =>
                permission.PermissionCode ==
                PermissionCodes.EmployeeView);

        Assert.Contains(
            persistence.Permissions,
            permission =>
                permission.PermissionCode ==
                PermissionCodes.EmployeeEdit);
    }

    [Fact]
    public async Task
        ReplaceAsync_WithUnknownPermission_DoesNotPersist()
    {
        var persistence =
            new TestPersistence();

        var service =
            new RolePermissionAssignmentService(
                new TestCurrentUserContext(
                    Guid.NewGuid()),
                persistence);

        RoleManagementResult result =
            await service.ReplaceAsync(
                Guid.NewGuid(),
                new[]
                {
                    "Unknown.Permission"
                });

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
                    RolePermissionAssignmentPersistenceResult
                        .PermissionEscalation
            };

        var service =
            new RolePermissionAssignmentService(
                new TestCurrentUserContext(
                    Guid.NewGuid()),
                persistence);

        RoleManagementResult result =
            await service.ReplaceAsync(
                Guid.NewGuid(),
                new[]
                {
                    PermissionCodes.PayrollClose
                });

        Assert.False(
            result.IsSuccessful);

        Assert.False(
            string.IsNullOrWhiteSpace(
                result.ErrorMessage));
    }

    [Fact]
    public async Task
        AuthorizedService_RequiresRoleAssignPermission()
    {
        var guard =
            new TestAuthorizationGuard();

        var inner =
            new TestAssignmentService();

        var service =
            new AuthorizedRolePermissionAssignmentService(
                inner,
                guard);

        await service.ReplaceAsync(
            Guid.NewGuid(),
            Array.Empty<string>());

        Assert.Equal(
            PermissionCodes.RoleAssignPermission,
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
            new AuthorizedRolePermissionAssignmentService(
                inner,
                new TestAuthorizationGuard(
                    deny:
                        true));

        await Assert.ThrowsAsync<
            AuthorizationDeniedException>(
                () =>
                    service.ReplaceAsync(
                        Guid.NewGuid(),
                        Array.Empty<string>()));

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
        : IRolePermissionAssignmentPersistence
    {
        public RolePermissionAssignmentPersistenceResult Result
        {
            get;
            set;
        } =
            RolePermissionAssignmentPersistenceResult.Updated;

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

        public Guid RoleId
        {
            get;
            private set;
        }

        public IReadOnlyCollection<RolePermission> Permissions
        {
            get;
            private set;
        } =
            Array.Empty<RolePermission>();

        public Task<RolePermissionAssignmentPersistenceResult>
            TryReplaceAsync(
                Guid actorAccountId,
                Guid roleId,
                IReadOnlyCollection<RolePermission> permissions,
                CancellationToken cancellationToken = default)
        {
            Called =
                true;

            ActorAccountId =
                actorAccountId;

            RoleId =
                roleId;

            Permissions =
                permissions;

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
        : IRolePermissionAssignmentService
    {
        public bool Called
        {
            get;
            private set;
        }

        public Task<RoleManagementResult> ReplaceAsync(
            Guid roleId,
            IReadOnlyCollection<string> permissionCodes,
            CancellationToken cancellationToken = default)
        {
            Called =
                true;

            return Task.FromResult(
                new RoleManagementResult(
                    true,
                    roleId));
        }
    }
}
