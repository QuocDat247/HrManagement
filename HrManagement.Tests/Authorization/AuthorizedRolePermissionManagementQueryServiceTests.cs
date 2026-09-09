using HrManagement.Application.Authorization;
using HrManagement.Application.Authorization.Roles;
using HrManagement.Domain.Authorization.Permissions;

namespace HrManagement.Tests.Authorization;

public sealed class AuthorizedRolePermissionManagementQueryServiceTests
{
    [Fact]
    public async Task
        GetAsync_RequiresRoleAssignPermissionAndDelegates()
    {
        Guid roleId =
            Guid.NewGuid();

        var snapshot =
            new RolePermissionManagementSnapshot(
                roleId,
                "Quản lý nhân sự",
                true,
                Array.Empty<string>(),
                Array.Empty<string>());

        var inner =
            new TestQueryService(
                snapshot);

        var guard =
            new TestAuthorizationGuard();

        var service =
            new AuthorizedRolePermissionManagementQueryService(
                inner,
                guard);

        RolePermissionManagementSnapshot? result =
            await service.GetAsync(
                roleId);

        Assert.Equal(
            PermissionCodes.RoleAssignPermission,
            guard.LastPermissionCode);

        Assert.True(
            inner.Called);

        Assert.Same(
            snapshot,
            result);
    }

    [Fact]
    public async Task
        GetAsync_WhenAuthorizationDenied_DoesNotCallInner()
    {
        Guid roleId =
            Guid.NewGuid();

        var inner =
            new TestQueryService(
                null);

        var guard =
            new TestAuthorizationGuard
            {
                Exception =
                    new AuthorizationDeniedException(
                        PermissionCodes.RoleAssignPermission)
            };

        var service =
            new AuthorizedRolePermissionManagementQueryService(
                inner,
                guard);

        await Assert.ThrowsAsync<
            AuthorizationDeniedException>(
                () =>
                    service.GetAsync(
                        roleId));

        Assert.False(
            inner.Called);
    }

    private sealed class TestQueryService
        : IRolePermissionManagementQueryService
    {
        private readonly RolePermissionManagementSnapshot?
            _snapshot;

        public TestQueryService(
            RolePermissionManagementSnapshot? snapshot)
        {
            _snapshot =
                snapshot;
        }

        public bool Called
        {
            get;
            private set;
        }

        public Task<RolePermissionManagementSnapshot?> GetAsync(
            Guid roleId,
            CancellationToken cancellationToken = default)
        {
            Called =
                true;

            return Task.FromResult(
                _snapshot);
        }
    }

    private sealed class TestAuthorizationGuard
        : IAuthorizationGuard
    {
        public string? LastPermissionCode
        {
            get;
            private set;
        }

        public Exception? Exception
        {
            get;
            set;
        }

        public Task RequirePermissionAsync(
            string permissionCode,
            CancellationToken cancellationToken = default)
        {
            LastPermissionCode =
                permissionCode;

            if (Exception is not null)
            {
                throw Exception;
            }

            return Task.CompletedTask;
        }
    }
}
