using HrManagement.Application.Authentication;
using HrManagement.Application.Authorization;
using HrManagement.Application.Overtime.Requests;
using HrManagement.Application.Workspaces.Overtime;
using HrManagement.Domain.Authorization.Permissions;
using HrManagement.Domain.Overtime.Requests;

namespace HrManagement.Tests.Authorization;

public sealed class AuthorizedOvertimeServiceTests
{
    [Fact]
    public async Task
        WorkspaceGet_RequiresViewPermission()
    {
        var guard =
            new TestAuthorizationGuard();

        var inner =
            new TestOvertimeWorkspaceQueryService();

        var service =
            new AuthorizedOvertimeWorkspaceQueryService(
                inner,
                guard);

        await service.GetAsync(
            default!);

        Assert.Equal(
            PermissionCodes.OvertimeView,
            guard.LastPermissionCode);

        Assert.True(
            inner.GetCalled);
    }

    [Fact]
    public async Task
        WorkspaceGetEmployees_RequiresViewPermission()
    {
        var guard =
            new TestAuthorizationGuard();

        var inner =
            new TestOvertimeWorkspaceQueryService();

        var service =
            new AuthorizedOvertimeWorkspaceQueryService(
                inner,
                guard);

        await service.GetEmployeesAsync();

        Assert.Equal(
            PermissionCodes.OvertimeView,
            guard.LastPermissionCode);

        Assert.True(
            inner.GetEmployeesCalled);
    }

    [Fact]
    public async Task
        WorkspaceGetHistory_RequiresViewPermission()
    {
        var guard =
            new TestAuthorizationGuard();

        var inner =
            new TestOvertimeWorkspaceQueryService();

        var service =
            new AuthorizedOvertimeWorkspaceQueryService(
                inner,
                guard);

        await service.GetHistoryAsync(
            Guid.NewGuid());

        Assert.Equal(
            PermissionCodes.OvertimeView,
            guard.LastPermissionCode);

        Assert.True(
            inner.GetHistoryCalled);
    }

    [Fact]
    public async Task
        WorkspaceDenied_DoesNotInvokeInner()
    {
        var inner =
            new TestOvertimeWorkspaceQueryService();

        var service =
            new AuthorizedOvertimeWorkspaceQueryService(
                inner,
                new TestAuthorizationGuard(
                    deny:
                        true));

        await Assert.ThrowsAsync<
            AuthorizationDeniedException>(
                () =>
                    service.GetAsync(
                        default!));

        Assert.False(
            inner.GetCalled);
    }

    [Fact]
    public async Task
        SubmissionPolicy_RequiresSubmitPermission()
    {
        var authorizationService =
            new TestAuthorizationService();

        var policy =
            new PermissionOvertimeRequestSubmissionAuthorizationPolicy(
                authorizationService);

        bool allowed =
            await policy.CanSubmitAsync(
                CreateSubmissionRequest());

        Assert.True(
            allowed);

        Assert.Equal(
            PermissionCodes.OvertimeSubmit,
            authorizationService.LastPermissionCode);
    }

    [Theory]
    [InlineData(OvertimeRequestStatus.Approved)]
    [InlineData(OvertimeRequestStatus.Rejected)]
    public async Task
        StatusPolicy_ReviewTargets_RequireReviewPermission(
            OvertimeRequestStatus targetStatus)
    {
        var authorizationService =
            new TestAuthorizationService();

        var policy =
            new PermissionOvertimeRequestStatusAuthorizationPolicy(
                authorizationService);

        bool allowed =
            await policy.CanChangeStatusAsync(
                CreateStatusRequest(
                    targetStatus));

        Assert.True(
            allowed);

        Assert.Equal(
            PermissionCodes.OvertimeReview,
            authorizationService.LastPermissionCode);
    }

    [Fact]
    public async Task
        StatusPolicy_Cancel_RequiresCancelPermission()
    {
        var authorizationService =
            new TestAuthorizationService();

        var policy =
            new PermissionOvertimeRequestStatusAuthorizationPolicy(
                authorizationService);

        bool allowed =
            await policy.CanChangeStatusAsync(
                CreateStatusRequest(
                    OvertimeRequestStatus.Cancelled));

        Assert.True(
            allowed);

        Assert.Equal(
            PermissionCodes.OvertimeCancel,
            authorizationService.LastPermissionCode);
    }

    [Fact]
    public async Task
        StatusPolicy_UnsupportedTarget_FailsClosed()
    {
        var authorizationService =
            new TestAuthorizationService();

        var policy =
            new PermissionOvertimeRequestStatusAuthorizationPolicy(
                authorizationService);

        bool allowed =
            await policy.CanChangeStatusAsync(
                CreateStatusRequest(
                    OvertimeRequestStatus.Pending));

        Assert.False(
            allowed);

        Assert.Null(
            authorizationService.LastPermissionCode);
    }

    private static OvertimeRequestSubmissionAuthorizationRequest
        CreateSubmissionRequest()
    {
        return new OvertimeRequestSubmissionAuthorizationRequest(
            CreateUser(),
            Guid.NewGuid(),
            new DateOnly(
                2026,
                9,
                9));
    }

    private static OvertimeRequestStatusAuthorizationRequest
        CreateStatusRequest(
            OvertimeRequestStatus targetStatus)
    {
        return new OvertimeRequestStatusAuthorizationRequest(
            CreateUser(),
            Guid.NewGuid(),
            targetStatus);
    }

    private static AuthenticatedUser CreateUser()
    {
        return new AuthenticatedUser(
            Guid.NewGuid()
                .ToString("D"),
            "test-user",
            "Test User");
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

    private sealed class TestAuthorizationService
        : IAuthorizationService
    {
        public string? LastPermissionCode
        {
            get;
            private set;
        }

        public Task<bool> HasPermissionAsync(
            string permissionCode,
            CancellationToken cancellationToken = default)
        {
            cancellationToken
                .ThrowIfCancellationRequested();

            LastPermissionCode =
                permissionCode;

            return Task.FromResult(
                true);
        }
    }

    private sealed class TestOvertimeWorkspaceQueryService
        : IOvertimeWorkspaceQueryService
    {
        public bool GetCalled { get; private set; }

        public bool GetEmployeesCalled { get; private set; }

        public bool GetHistoryCalled { get; private set; }

        public Task<OvertimeWorkspaceSnapshot> GetAsync(
            OvertimeWorkspaceQuery query,
            CancellationToken cancellationToken = default)
        {
            GetCalled =
                true;

            return Task.FromResult(
                default(OvertimeWorkspaceSnapshot)!);
        }

        public Task<IReadOnlyList<OvertimeEmployeeOption>>
            GetEmployeesAsync(
                CancellationToken cancellationToken = default)
        {
            GetEmployeesCalled =
                true;

            return Task.FromResult<
                IReadOnlyList<OvertimeEmployeeOption>>(
                    []);
        }

        public Task<IReadOnlyList<OvertimeStatusHistoryItem>>
            GetHistoryAsync(
                Guid overtimeRequestId,
                CancellationToken cancellationToken = default)
        {
            GetHistoryCalled =
                true;

            return Task.FromResult<
                IReadOnlyList<OvertimeStatusHistoryItem>>(
                    []);
        }
    }
}
