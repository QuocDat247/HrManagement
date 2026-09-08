using HrManagement.Application.Attendance.Schedules;
using HrManagement.Application.Authorization;
using HrManagement.Application.Workspaces.WorkSchedules;
using HrManagement.Domain.Authorization.Permissions;

namespace HrManagement.Tests.Authorization;

public sealed class AuthorizedWorkScheduleServiceTests
{
    [Fact]
    public async Task
        WorkspaceGetEmployees_RequiresViewPermission()
    {
        var guard =
            new TestAuthorizationGuard();

        var inner =
            new TestWorkScheduleWorkspaceQueryService();

        var service =
            new AuthorizedWorkScheduleWorkspaceQueryService(
                inner,
                guard);

        await service.GetEmployeesAsync();

        Assert.Equal(
            PermissionCodes.WorkScheduleView,
            guard.LastPermissionCode);

        Assert.True(
            inner.GetEmployeesCalled);
    }

    [Fact]
    public async Task
        WorkspaceGet_RequiresViewPermission()
    {
        var guard =
            new TestAuthorizationGuard();

        var inner =
            new TestWorkScheduleWorkspaceQueryService();

        var service =
            new AuthorizedWorkScheduleWorkspaceQueryService(
                inner,
                guard);

        await service.GetAsync(
            default!);

        Assert.Equal(
            PermissionCodes.WorkScheduleView,
            guard.LastPermissionCode);

        Assert.True(
            inner.GetCalled);
    }

    [Fact]
    public async Task
        Create_RequiresCreatePermission()
    {
        var guard =
            new TestAuthorizationGuard();

        var inner =
            new TestWorkScheduleManagementService();

        var service =
            new AuthorizedWorkScheduleManagementService(
                inner,
                guard);

        await service.CreateAsync(
            default!);

        Assert.Equal(
            PermissionCodes.WorkScheduleCreate,
            guard.LastPermissionCode);

        Assert.True(
            inner.CreateCalled);
    }

    [Fact]
    public async Task
        Clone_RequiresCreatePermission()
    {
        var guard =
            new TestAuthorizationGuard();

        var inner =
            new TestWorkScheduleManagementService();

        var service =
            new AuthorizedWorkScheduleManagementService(
                inner,
                guard);

        await service.CloneAsync(
            default!);

        Assert.Equal(
            PermissionCodes.WorkScheduleCreate,
            guard.LastPermissionCode);

        Assert.True(
            inner.CloneCalled);
    }

    [Fact]
    public async Task
        Update_RequiresEditPermission()
    {
        var guard =
            new TestAuthorizationGuard();

        var inner =
            new TestWorkScheduleManagementService();

        var service =
            new AuthorizedWorkScheduleManagementService(
                inner,
                guard);

        await service.UpdateAsync(
            default!);

        Assert.Equal(
            PermissionCodes.WorkScheduleEdit,
            guard.LastPermissionCode);

        Assert.True(
            inner.UpdateCalled);
    }

    [Fact]
    public async Task
        Deactivate_RequiresLifecyclePermission()
    {
        var guard =
            new TestAuthorizationGuard();

        var inner =
            new TestWorkScheduleManagementService();

        var service =
            new AuthorizedWorkScheduleManagementService(
                inner,
                guard);

        await service.DeactivateAsync(
            Guid.NewGuid());

        Assert.Equal(
            PermissionCodes.WorkScheduleManageLifecycle,
            guard.LastPermissionCode);

        Assert.True(
            inner.DeactivateCalled);
    }

    [Fact]
    public async Task
        Reactivate_RequiresLifecyclePermission()
    {
        var guard =
            new TestAuthorizationGuard();

        var inner =
            new TestWorkScheduleManagementService();

        var service =
            new AuthorizedWorkScheduleManagementService(
                inner,
                guard);

        await service.ReactivateAsync(
            Guid.NewGuid());

        Assert.Equal(
            PermissionCodes.WorkScheduleManageLifecycle,
            guard.LastPermissionCode);

        Assert.True(
            inner.ReactivateCalled);
    }

    [Fact]
    public async Task
        Delete_RequiresDeletePermission()
    {
        var guard =
            new TestAuthorizationGuard();

        var inner =
            new TestWorkScheduleManagementService();

        var service =
            new AuthorizedWorkScheduleManagementService(
                inner,
                guard);

        await service.DeleteAsync(
            Guid.NewGuid());

        Assert.Equal(
            PermissionCodes.WorkScheduleDelete,
            guard.LastPermissionCode);

        Assert.True(
            inner.DeleteCalled);
    }

    [Fact]
    public async Task
        UpdateDay_RequiresEditPermission()
    {
        var guard =
            new TestAuthorizationGuard();

        var inner =
            new TestWorkScheduleDayManagementService();

        var service =
            new AuthorizedWorkScheduleDayManagementService(
                inner,
                guard);

        await service.UpdateAsync(
            default!);

        Assert.Equal(
            PermissionCodes.WorkScheduleEdit,
            guard.LastPermissionCode);

        Assert.True(
            inner.UpdateCalled);
    }

    [Fact]
    public async Task
        Assign_RequiresAssignPermission()
    {
        var guard =
            new TestAuthorizationGuard();

        var inner =
            new TestEmployeeWorkScheduleAssignmentService();

        var service =
            new AuthorizedEmployeeWorkScheduleAssignmentService(
                inner,
                guard);

        await service.AssignAsync(
            default!);

        Assert.Equal(
            PermissionCodes.WorkScheduleAssign,
            guard.LastPermissionCode);

        Assert.True(
            inner.AssignCalled);
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
            new TestWorkScheduleManagementService();

        var service =
            new AuthorizedWorkScheduleManagementService(
                inner,
                guard);

        await Assert.ThrowsAsync<
            AuthorizationDeniedException>(
                () =>
                    service.DeleteAsync(
                        Guid.NewGuid()));

        Assert.False(
            inner.DeleteCalled);
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

    private sealed class TestWorkScheduleWorkspaceQueryService
        : IWorkScheduleWorkspaceQueryService
    {
        public bool GetEmployeesCalled
        {
            get;
            private set;
        }

        public bool GetCalled
        {
            get;
            private set;
        }

        public Task<IReadOnlyList<WorkScheduleWorkspaceEmployeeItem>>
            GetEmployeesAsync(
                CancellationToken cancellationToken = default)
        {
            GetEmployeesCalled =
                true;

            return Task.FromResult<
                IReadOnlyList<WorkScheduleWorkspaceEmployeeItem>>(
                    Array.Empty<
                        WorkScheduleWorkspaceEmployeeItem>());
        }

        public Task<WorkScheduleWorkspaceSnapshot>
            GetAsync(
                WorkScheduleWorkspaceQuery query,
                CancellationToken cancellationToken = default)
        {
            GetCalled =
                true;

            return Task.FromResult(
                default(WorkScheduleWorkspaceSnapshot)!);
        }
    }

    private sealed class TestWorkScheduleManagementService
        : IWorkScheduleManagementService
    {
        public bool CreateCalled
        {
            get;
            private set;
        }

        public bool CloneCalled
        {
            get;
            private set;
        }

        public bool UpdateCalled
        {
            get;
            private set;
        }

        public bool DeactivateCalled
        {
            get;
            private set;
        }

        public bool ReactivateCalled
        {
            get;
            private set;
        }

        public bool DeleteCalled
        {
            get;
            private set;
        }

        public Task<WorkScheduleManagementResult>
            CreateAsync(
                CreateWorkScheduleRequest request,
                CancellationToken cancellationToken = default)
        {
            CreateCalled =
                true;

            return Task.FromResult(
                default(WorkScheduleManagementResult)!);
        }

        public Task<WorkScheduleManagementResult>
            CloneAsync(
                CloneWorkScheduleRequest request,
                CancellationToken cancellationToken = default)
        {
            CloneCalled =
                true;

            return Task.FromResult(
                default(WorkScheduleManagementResult)!);
        }

        public Task<WorkScheduleManagementResult>
            UpdateAsync(
                UpdateWorkScheduleRequest request,
                CancellationToken cancellationToken = default)
        {
            UpdateCalled =
                true;

            return Task.FromResult(
                default(WorkScheduleManagementResult)!);
        }

        public Task<WorkScheduleManagementResult>
            DeactivateAsync(
                Guid workScheduleId,
                CancellationToken cancellationToken = default)
        {
            DeactivateCalled =
                true;

            return Task.FromResult(
                default(WorkScheduleManagementResult)!);
        }

        public Task<WorkScheduleManagementResult>
            ReactivateAsync(
                Guid workScheduleId,
                CancellationToken cancellationToken = default)
        {
            ReactivateCalled =
                true;

            return Task.FromResult(
                default(WorkScheduleManagementResult)!);
        }

        public Task<WorkScheduleManagementResult>
            DeleteAsync(
                Guid workScheduleId,
                CancellationToken cancellationToken = default)
        {
            DeleteCalled =
                true;

            return Task.FromResult(
                default(WorkScheduleManagementResult)!);
        }
    }

    private sealed class TestWorkScheduleDayManagementService
        : IWorkScheduleDayManagementService
    {
        public bool UpdateCalled
        {
            get;
            private set;
        }

        public Task<WorkScheduleDayManagementResult>
            UpdateAsync(
                UpdateWorkScheduleDayRequest request,
                CancellationToken cancellationToken = default)
        {
            UpdateCalled =
                true;

            return Task.FromResult(
                default(WorkScheduleDayManagementResult)!);
        }
    }

    private sealed class TestEmployeeWorkScheduleAssignmentService
        : IEmployeeWorkScheduleAssignmentService
    {
        public bool AssignCalled
        {
            get;
            private set;
        }

        public Task<AssignEmployeeWorkScheduleResult>
            AssignAsync(
                AssignEmployeeWorkScheduleRequest request,
                CancellationToken cancellationToken = default)
        {
            AssignCalled =
                true;

            return Task.FromResult(
                default(AssignEmployeeWorkScheduleResult)!);
        }
    }
}
