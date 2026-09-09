using HrManagement.Application.Authorization;
using HrManagement.Application.Authorization.Roles;
using HrManagement.Domain.Authorization.Permissions;

namespace HrManagement.Tests.Authorization;

public sealed class RoleActiveStateServiceTests
{
    [Theory]
    [InlineData(
        RoleActiveStatePersistenceResult.Updated)]
    [InlineData(
        RoleActiveStatePersistenceResult.Unchanged)]
    public async Task
        SetAsync_WhenPersistenceAccepts_ReturnsSuccess(
            RoleActiveStatePersistenceResult persistenceResult)
    {
        var persistence =
            new TestPersistence
            {
                Result =
                    persistenceResult
            };

        var service =
            new RoleActiveStateService(
                persistence);

        Guid roleId =
            Guid.NewGuid();

        RoleManagementResult result =
            await service.SetAsync(
                roleId,
                false);

        Assert.True(
            result.IsSuccessful);

        Assert.Equal(
            roleId,
            result.RoleId);

        Assert.Equal(
            roleId,
            persistence.RoleId);

        Assert.False(
            persistence.IsActive);
    }

    [Fact]
    public async Task
        SetAsync_WhenRoleNotFound_ReturnsFailure()
    {
        var persistence =
            new TestPersistence
            {
                Result =
                    RoleActiveStatePersistenceResult
                        .RoleNotFound
            };

        var service =
            new RoleActiveStateService(
                persistence);

        RoleManagementResult result =
            await service.SetAsync(
                Guid.NewGuid(),
                false);

        Assert.False(
            result.IsSuccessful);

        Assert.False(
            string.IsNullOrWhiteSpace(
                result.ErrorMessage));
    }

    [Fact]
    public async Task
        SetAsync_WithEmptyRoleId_DoesNotPersist()
    {
        var persistence =
            new TestPersistence();

        var service =
            new RoleActiveStateService(
                persistence);

        RoleManagementResult result =
            await service.SetAsync(
                Guid.Empty,
                false);

        Assert.False(
            result.IsSuccessful);

        Assert.False(
            persistence.Called);
    }

    [Fact]
    public async Task
        AuthorizedService_RequiresRoleManageLifecycle()
    {
        var guard =
            new TestAuthorizationGuard();

        var inner =
            new TestRoleActiveStateService();

        var service =
            new AuthorizedRoleActiveStateService(
                inner,
                guard);

        await service.SetAsync(
            Guid.NewGuid(),
            false);

        Assert.Equal(
            PermissionCodes.RoleManageLifecycle,
            guard.LastPermissionCode);

        Assert.True(
            inner.Called);
    }

    [Fact]
    public async Task
        AuthorizedService_WhenDenied_DoesNotInvokeInner()
    {
        var inner =
            new TestRoleActiveStateService();

        var service =
            new AuthorizedRoleActiveStateService(
                inner,
                new TestAuthorizationGuard(
                    deny:
                        true));

        await Assert.ThrowsAsync<
            AuthorizationDeniedException>(
                () =>
                    service.SetAsync(
                        Guid.NewGuid(),
                        false));

        Assert.False(
            inner.Called);
    }

    private sealed class TestPersistence
        : IRoleActiveStatePersistence
    {
        public RoleActiveStatePersistenceResult Result
        {
            get;
            set;
        } =
            RoleActiveStatePersistenceResult.Updated;

        public bool Called
        {
            get;
            private set;
        }

        public Guid RoleId
        {
            get;
            private set;
        }

        public bool IsActive
        {
            get;
            private set;
        }

        public Task<RoleActiveStatePersistenceResult> TrySetAsync(
            Guid roleId,
            bool isActive,
            CancellationToken cancellationToken = default)
        {
            Called =
                true;

            RoleId =
                roleId;

            IsActive =
                isActive;

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
            cancellationToken
                .ThrowIfCancellationRequested();

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

    private sealed class TestRoleActiveStateService
        : IRoleActiveStateService
    {
        public bool Called
        {
            get;
            private set;
        }

        public Task<RoleManagementResult> SetAsync(
            Guid roleId,
            bool isActive,
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
