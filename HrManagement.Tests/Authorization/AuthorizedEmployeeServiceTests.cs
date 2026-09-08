using HrManagement.Application.Authorization;
using HrManagement.Application.Employees;
using HrManagement.Domain.Authorization.Permissions;
using HrManagement.Domain.Employees;

namespace HrManagement.Tests.Authorization;

public sealed class AuthorizedEmployeeServiceTests
{
    [Fact]
    public async Task
        GetEmployeesAsync_RequiresViewPermission()
    {
        var guard =
            new TestAuthorizationGuard();

        var inner =
            new TestEmployeeService();

        var service =
            new AuthorizedEmployeeService(
                inner,
                guard);

        await service.GetEmployeesAsync();

        Assert.Equal(
            PermissionCodes.EmployeeView,
            guard.LastPermissionCode);

        Assert.True(
            inner.GetEmployeesCalled);
    }

    [Fact]
    public async Task
        CreateEmployeeAsync_RequiresCreatePermission()
    {
        var guard =
            new TestAuthorizationGuard();

        var inner =
            new TestEmployeeService();

        var service =
            new AuthorizedEmployeeService(
                inner,
                guard);

        await service.CreateEmployeeAsync(
            null!);

        Assert.Equal(
            PermissionCodes.EmployeeCreate,
            guard.LastPermissionCode);

        Assert.True(
            inner.CreateEmployeeCalled);
    }

    [Fact]
    public async Task
        UpdateEmployeeAsync_RequiresEditPermission()
    {
        var guard =
            new TestAuthorizationGuard();

        var inner =
            new TestEmployeeService();

        var service =
            new AuthorizedEmployeeService(
                inner,
                guard);

        await service.UpdateEmployeeAsync(
            null!);

        Assert.Equal(
            PermissionCodes.EmployeeEdit,
            guard.LastPermissionCode);

        Assert.True(
            inner.UpdateEmployeeCalled);
    }

    [Fact]
    public async Task
        DeactivateEmployeeAsync_RequiresDeletePermission()
    {
        var guard =
            new TestAuthorizationGuard();

        var inner =
            new TestEmployeeService();

        var service =
            new AuthorizedEmployeeService(
                inner,
                guard);

        await service.DeactivateEmployeeAsync(
            Guid.NewGuid());

        Assert.Equal(
            PermissionCodes.EmployeeManageLifecycle,
            guard.LastPermissionCode);

        Assert.True(
            inner.DeactivateEmployeeCalled);
    }

    [Fact]
    public async Task
        CancelDeactivationAsync_RequiresManageLifecyclePermission()
    {
        var guard =
            new TestAuthorizationGuard();

        var inner =
            new TestEmployeeService();

        var service =
            new AuthorizedEmployeeService(
                inner,
                guard);

        await service.CancelDeactivationAsync(
            Guid.NewGuid(),
            EmployeeStatus.Active);

        Assert.Equal(
            PermissionCodes.EmployeeEdit,
            guard.LastPermissionCode);

        Assert.True(
            inner.CancelDeactivationCalled);
    }

    [Fact]
    public async Task
        RehireEmployeeAsync_RequiresManageLifecyclePermission()
    {
        var guard =
            new TestAuthorizationGuard();

        var inner =
            new TestEmployeeService();

        var service =
            new AuthorizedEmployeeService(
                inner,
                guard);

        await service.RehireEmployeeAsync(
            Guid.NewGuid(),
            new DateOnly(
                2026,
                9,
                8),
            EmployeeStatus.Active);

        Assert.Equal(
            PermissionCodes.EmployeeEdit,
            guard.LastPermissionCode);

        Assert.True(
            inner.RehireEmployeeCalled);
    }

    [Fact]
    public async Task
        DeniedPermission_DoesNotInvokeInnerService()
    {
        var guard =
            new TestAuthorizationGuard(
                deny:
                    true);

        var inner =
            new TestEmployeeService();

        var service =
            new AuthorizedEmployeeService(
                inner,
                guard);

        await Assert.ThrowsAsync<
            AuthorizationDeniedException>(
                () =>
                    service.GetEmployeesAsync());

        Assert.False(
            inner.GetEmployeesCalled);
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

    private sealed class TestEmployeeService
        : IEmployeeService
    {
        public bool GetEmployeesCalled
        {
            get;
            private set;
        }

        public bool CreateEmployeeCalled
        {
            get;
            private set;
        }

        public bool UpdateEmployeeCalled
        {
            get;
            private set;
        }

        public bool DeactivateEmployeeCalled
        {
            get;
            private set;
        }

        public bool CancelDeactivationCalled
        {
            get;
            private set;
        }

        public bool RehireEmployeeCalled
        {
            get;
            private set;
        }

        public Task<IReadOnlyList<Employee>>
            GetEmployeesAsync(
                EmployeeFilter? filter = null,
                CancellationToken cancellationToken = default)
        {
            GetEmployeesCalled =
                true;

            return Task.FromResult<
                IReadOnlyList<Employee>>(
                    Array.Empty<Employee>());
        }

        public Task<CreateEmployeeResult>
            CreateEmployeeAsync(
                CreateEmployeeRequest request,
                CancellationToken cancellationToken = default)
        {
            CreateEmployeeCalled =
                true;

            return Task.FromResult(
                default(CreateEmployeeResult)!);
        }

        public Task<UpdateEmployeeResult>
            UpdateEmployeeAsync(
                UpdateEmployeeRequest request,
                CancellationToken cancellationToken = default)
        {
            UpdateEmployeeCalled =
                true;

            return Task.FromResult(
                default(UpdateEmployeeResult)!);
        }

        public Task<DeactivateEmployeeResult>
            DeactivateEmployeeAsync(
                Guid employeeId,
                DateOnly? terminationDate = null,
                CancellationToken cancellationToken = default)
        {
            DeactivateEmployeeCalled =
                true;

            return Task.FromResult(
                default(DeactivateEmployeeResult)!);
        }

        public Task<CancelEmployeeDeactivationResult>
            CancelDeactivationAsync(
                Guid employeeId,
                EmployeeStatus restoredStatus,
                CancellationToken cancellationToken = default)
        {
            CancelDeactivationCalled =
                true;

            return Task.FromResult(
                default(CancelEmployeeDeactivationResult)!);
        }

        public Task<RehireEmployeeResult>
            RehireEmployeeAsync(
                Guid employeeId,
                DateOnly rehireDate,
                EmployeeStatus rehireStatus,
                CancellationToken cancellationToken = default)
        {
            RehireEmployeeCalled =
                true;

            return Task.FromResult(
                default(RehireEmployeeResult)!);
        }
    }
}
