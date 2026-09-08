using HrManagement.Application.Attendance.Calendars;
using HrManagement.Application.Authorization;
using HrManagement.Application.Workspaces.HolidayExceptions;
using HrManagement.Domain.Authorization.Permissions;

namespace HrManagement.Tests.Authorization;

public sealed class AuthorizedHolidayExceptionServiceTests
{
    [Fact]
    public async Task
        WorkspaceGet_RequiresViewPermission()
    {
        var guard =
            new TestAuthorizationGuard();

        var inner =
            new TestHolidayExceptionWorkspaceQueryService();

        var service =
            new AuthorizedHolidayExceptionWorkspaceQueryService(
                inner,
                guard);

        await service.GetAsync(
            default!);

        Assert.Equal(
            PermissionCodes.HolidayExceptionView,
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
            new TestHolidayCalendarManagementService();

        var service =
            new AuthorizedHolidayCalendarManagementService(
                inner,
                guard);

        await service.CreateAsync(
            default!);

        Assert.Equal(
            PermissionCodes.HolidayExceptionCreate,
            guard.LastPermissionCode);

        Assert.True(
            inner.CreateCalled);
    }

    [Fact]
    public async Task
        Rename_RequiresEditPermission()
    {
        var guard =
            new TestAuthorizationGuard();

        var inner =
            new TestHolidayCalendarManagementService();

        var service =
            new AuthorizedHolidayCalendarManagementService(
                inner,
                guard);

        await service.RenameAsync(
            default!);

        Assert.Equal(
            PermissionCodes.HolidayExceptionEdit,
            guard.LastPermissionCode);

        Assert.True(
            inner.RenameCalled);
    }

    [Fact]
    public async Task
        Deactivate_RequiresLifecyclePermission()
    {
        var guard =
            new TestAuthorizationGuard();

        var inner =
            new TestHolidayCalendarManagementService();

        var service =
            new AuthorizedHolidayCalendarManagementService(
                inner,
                guard);

        await service.DeactivateAsync(
            Guid.NewGuid());

        Assert.Equal(
            PermissionCodes.HolidayExceptionManageLifecycle,
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
            new TestHolidayCalendarManagementService();

        var service =
            new AuthorizedHolidayCalendarManagementService(
                inner,
                guard);

        await service.ReactivateAsync(
            Guid.NewGuid());

        Assert.Equal(
            PermissionCodes.HolidayExceptionManageLifecycle,
            guard.LastPermissionCode);

        Assert.True(
            inner.ReactivateCalled);
    }

    [Fact]
    public async Task
        DeniedPermission_DoesNotInvokeInner()
    {
        var inner =
            new TestHolidayCalendarManagementService();

        var service =
            new AuthorizedHolidayCalendarManagementService(
                inner,
                new TestAuthorizationGuard(
                    deny:
                        true));

        await Assert.ThrowsAsync<
            AuthorizationDeniedException>(
                () =>
                    service.CreateAsync(
                        default!));

        Assert.False(
            inner.CreateCalled);
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

    private sealed class TestHolidayExceptionWorkspaceQueryService
        : IHolidayExceptionWorkspaceQueryService
    {
        public bool GetCalled
        {
            get;
            private set;
        }

        public Task<HolidayExceptionWorkspaceSnapshot> GetAsync(
            HolidayExceptionWorkspaceQuery query,
            CancellationToken cancellationToken = default)
        {
            GetCalled =
                true;

            return Task.FromResult(
                default(HolidayExceptionWorkspaceSnapshot)!);
        }
    }

    private sealed class TestHolidayCalendarManagementService
        : IHolidayCalendarManagementService
    {
        public bool CreateCalled { get; private set; }
        public bool RenameCalled { get; private set; }
        public bool DeactivateCalled { get; private set; }
        public bool ReactivateCalled { get; private set; }

        public Task<HolidayCalendarManagementResult> CreateAsync(
            CreateHolidayCalendarDayRequest request,
            CancellationToken cancellationToken = default)
        {
            CreateCalled =
                true;

            return Task.FromResult(
                default(HolidayCalendarManagementResult)!);
        }

        public Task<HolidayCalendarManagementResult> RenameAsync(
            RenameHolidayCalendarDayRequest request,
            CancellationToken cancellationToken = default)
        {
            RenameCalled =
                true;

            return Task.FromResult(
                default(HolidayCalendarManagementResult)!);
        }

        public Task<HolidayCalendarManagementResult> DeactivateAsync(
            Guid holidayCalendarDayId,
            CancellationToken cancellationToken = default)
        {
            DeactivateCalled =
                true;

            return Task.FromResult(
                default(HolidayCalendarManagementResult)!);
        }

        public Task<HolidayCalendarManagementResult> ReactivateAsync(
            Guid holidayCalendarDayId,
            CancellationToken cancellationToken = default)
        {
            ReactivateCalled =
                true;

            return Task.FromResult(
                default(HolidayCalendarManagementResult)!);
        }
    }
}
