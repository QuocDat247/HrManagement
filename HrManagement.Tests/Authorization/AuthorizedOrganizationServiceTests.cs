using HrManagement.Application.Authorization;
using HrManagement.Application.Organization.Departments;
using HrManagement.Application.Organization.Positions;
using HrManagement.Domain.Authorization.Permissions;
using HrManagement.Domain.Organization.Departments;
using HrManagement.Domain.Organization.Positions;

namespace HrManagement.Tests.Authorization;

public sealed class AuthorizedOrganizationServiceTests
{
    [Fact]
    public async Task
        DepartmentGet_RequiresViewPermission()
    {
        var guard =
            new TestAuthorizationGuard();

        var inner =
            new TestDepartmentService();

        var service =
            new AuthorizedDepartmentService(
                inner,
                guard);

        await service.GetDepartmentsAsync();

        Assert.Equal(
            PermissionCodes.DepartmentView,
            guard.LastPermissionCode);

        Assert.True(
            inner.GetCalled);
    }

    [Fact]
    public async Task
        DepartmentCreate_RequiresCreatePermission()
    {
        var guard =
            new TestAuthorizationGuard();

        var inner =
            new TestDepartmentService();

        var service =
            new AuthorizedDepartmentService(
                inner,
                guard);

        await service.CreateDepartmentAsync(
            null!);

        Assert.Equal(
            PermissionCodes.DepartmentCreate,
            guard.LastPermissionCode);

        Assert.True(
            inner.CreateCalled);
    }

    [Fact]
    public async Task
        DepartmentUpdate_RequiresEditPermission()
    {
        var guard =
            new TestAuthorizationGuard();

        var inner =
            new TestDepartmentService();

        var service =
            new AuthorizedDepartmentService(
                inner,
                guard);

        await service.UpdateDepartmentAsync(
            null!);

        Assert.Equal(
            PermissionCodes.DepartmentEdit,
            guard.LastPermissionCode);

        Assert.True(
            inner.UpdateCalled);
    }

    [Fact]
    public async Task
        DepartmentDeactivate_RequiresLifecyclePermission()
    {
        var guard =
            new TestAuthorizationGuard();

        var inner =
            new TestDepartmentService();

        var service =
            new AuthorizedDepartmentService(
                inner,
                guard);

        await service.DeactivateDepartmentAsync(
            Guid.NewGuid());

        Assert.Equal(
            PermissionCodes.DepartmentManageLifecycle,
            guard.LastPermissionCode);

        Assert.True(
            inner.DeactivateCalled);
    }

    [Fact]
    public async Task
        DepartmentReactivate_RequiresLifecyclePermission()
    {
        var guard =
            new TestAuthorizationGuard();

        var inner =
            new TestDepartmentService();

        var service =
            new AuthorizedDepartmentService(
                inner,
                guard);

        await service.ReactivateDepartmentAsync(
            Guid.NewGuid());

        Assert.Equal(
            PermissionCodes.DepartmentManageLifecycle,
            guard.LastPermissionCode);

        Assert.True(
            inner.ReactivateCalled);
    }

    [Fact]
    public async Task
        PositionGet_RequiresViewPermission()
    {
        var guard =
            new TestAuthorizationGuard();

        var inner =
            new TestPositionService();

        var service =
            new AuthorizedPositionService(
                inner,
                guard);

        await service.GetPositionsAsync();

        Assert.Equal(
            PermissionCodes.PositionView,
            guard.LastPermissionCode);

        Assert.True(
            inner.GetCalled);
    }

    [Fact]
    public async Task
        PositionCreate_RequiresCreatePermission()
    {
        var guard =
            new TestAuthorizationGuard();

        var inner =
            new TestPositionService();

        var service =
            new AuthorizedPositionService(
                inner,
                guard);

        await service.CreatePositionAsync(
            null!);

        Assert.Equal(
            PermissionCodes.PositionCreate,
            guard.LastPermissionCode);

        Assert.True(
            inner.CreateCalled);
    }

    [Fact]
    public async Task
        PositionUpdate_RequiresEditPermission()
    {
        var guard =
            new TestAuthorizationGuard();

        var inner =
            new TestPositionService();

        var service =
            new AuthorizedPositionService(
                inner,
                guard);

        await service.UpdatePositionAsync(
            null!);

        Assert.Equal(
            PermissionCodes.PositionEdit,
            guard.LastPermissionCode);

        Assert.True(
            inner.UpdateCalled);
    }

    [Fact]
    public async Task
        PositionDeactivate_RequiresLifecyclePermission()
    {
        var guard =
            new TestAuthorizationGuard();

        var inner =
            new TestPositionService();

        var service =
            new AuthorizedPositionService(
                inner,
                guard);

        await service.DeactivatePositionAsync(
            Guid.NewGuid());

        Assert.Equal(
            PermissionCodes.PositionManageLifecycle,
            guard.LastPermissionCode);

        Assert.True(
            inner.DeactivateCalled);
    }

    [Fact]
    public async Task
        PositionReactivate_RequiresLifecyclePermission()
    {
        var guard =
            new TestAuthorizationGuard();

        var inner =
            new TestPositionService();

        var service =
            new AuthorizedPositionService(
                inner,
                guard);

        await service.ReactivatePositionAsync(
            Guid.NewGuid());

        Assert.Equal(
            PermissionCodes.PositionManageLifecycle,
            guard.LastPermissionCode);

        Assert.True(
            inner.ReactivateCalled);
    }

    [Fact]
    public async Task
        DeniedDepartmentPermission_DoesNotInvokeInner()
    {
        var inner =
            new TestDepartmentService();

        var service =
            new AuthorizedDepartmentService(
                inner,
                new TestAuthorizationGuard(
                    deny:
                        true));

        await Assert.ThrowsAsync<
            AuthorizationDeniedException>(
                () =>
                    service.GetDepartmentsAsync());

        Assert.False(
            inner.GetCalled);
    }

    [Fact]
    public async Task
        DeniedPositionPermission_DoesNotInvokeInner()
    {
        var inner =
            new TestPositionService();

        var service =
            new AuthorizedPositionService(
                inner,
                new TestAuthorizationGuard(
                    deny:
                        true));

        await Assert.ThrowsAsync<
            AuthorizationDeniedException>(
                () =>
                    service.GetPositionsAsync());

        Assert.False(
            inner.GetCalled);
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

    private sealed class TestDepartmentService
        : IDepartmentService
    {
        public bool GetCalled { get; private set; }
        public bool CreateCalled { get; private set; }
        public bool UpdateCalled { get; private set; }
        public bool DeactivateCalled { get; private set; }
        public bool ReactivateCalled { get; private set; }

        public Task<IReadOnlyList<Department>>
            GetDepartmentsAsync(
                CancellationToken cancellationToken = default)
        {
            GetCalled = true;

            return Task.FromResult<
                IReadOnlyList<Department>>(
                    Array.Empty<Department>());
        }

        public Task<DepartmentOperationResult>
            CreateDepartmentAsync(
                CreateDepartmentRequest request,
                CancellationToken cancellationToken = default)
        {
            CreateCalled = true;

            return Task.FromResult(
                new DepartmentOperationResult(
                    true,
                    null));
        }

        public Task<DepartmentOperationResult>
            UpdateDepartmentAsync(
                UpdateDepartmentRequest request,
                CancellationToken cancellationToken = default)
        {
            UpdateCalled = true;

            return Task.FromResult(
                new DepartmentOperationResult(
                    true,
                    null));
        }

        public Task<DepartmentOperationResult>
            DeactivateDepartmentAsync(
                Guid departmentId,
                CancellationToken cancellationToken = default)
        {
            DeactivateCalled = true;

            return Task.FromResult(
                new DepartmentOperationResult(
                    true,
                    null));
        }

        public Task<DepartmentOperationResult>
            ReactivateDepartmentAsync(
                Guid departmentId,
                CancellationToken cancellationToken = default)
        {
            ReactivateCalled = true;

            return Task.FromResult(
                new DepartmentOperationResult(
                    true,
                    null));
        }
    }

    private sealed class TestPositionService
        : IPositionService
    {
        public bool GetCalled { get; private set; }
        public bool CreateCalled { get; private set; }
        public bool UpdateCalled { get; private set; }
        public bool DeactivateCalled { get; private set; }
        public bool ReactivateCalled { get; private set; }

        public Task<IReadOnlyList<Position>>
            GetPositionsAsync(
                CancellationToken cancellationToken = default)
        {
            GetCalled = true;

            return Task.FromResult<
                IReadOnlyList<Position>>(
                    Array.Empty<Position>());
        }

        public Task<PositionOperationResult>
            CreatePositionAsync(
                CreatePositionRequest request,
                CancellationToken cancellationToken = default)
        {
            CreateCalled = true;

            return Task.FromResult(
                new PositionOperationResult(
                    true,
                    null));
        }

        public Task<PositionOperationResult>
            UpdatePositionAsync(
                UpdatePositionRequest request,
                CancellationToken cancellationToken = default)
        {
            UpdateCalled = true;

            return Task.FromResult(
                new PositionOperationResult(
                    true,
                    null));
        }

        public Task<PositionOperationResult>
            DeactivatePositionAsync(
                Guid positionId,
                CancellationToken cancellationToken = default)
        {
            DeactivateCalled = true;

            return Task.FromResult(
                new PositionOperationResult(
                    true,
                    null));
        }

        public Task<PositionOperationResult>
            ReactivatePositionAsync(
                Guid positionId,
                CancellationToken cancellationToken = default)
        {
            ReactivateCalled = true;

            return Task.FromResult(
                new PositionOperationResult(
                    true,
                    null));
        }
    }
}
