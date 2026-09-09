using HrManagement.Application.Authorization;
using HrManagement.Application.Leave.Requests;
using HrManagement.Application.Workspaces.AttendanceLeave;
using HrManagement.Domain.Authorization.Permissions;
using HrManagement.Domain.Leave.Requests;

namespace HrManagement.Tests.Authorization;

public sealed class AuthorizedLeaveServiceTests
{
    [Fact]
    public async Task
        Submission_RequiresLeaveSubmit()
    {
        var guard =
            new TestAuthorizationGuard();

        var inner =
            new TestLeaveRequestSubmissionService();

        var service =
            new AuthorizedLeaveRequestSubmissionService(
                inner,
                guard);

        await service.SubmitAsync(
            default!);

        Assert.Equal(
            PermissionCodes.LeaveSubmit,
            Assert.Single(
                guard.PermissionCodes));

        Assert.True(
            inner.Called);
    }

    [Theory]
    [InlineData(LeaveRequestStatus.Approved)]
    [InlineData(LeaveRequestStatus.Rejected)]
    public async Task
        ReviewTargets_RequireLeaveApprove(
            LeaveRequestStatus targetStatus)
    {
        var guard =
            new TestAuthorizationGuard();

        var service =
            new AuthorizedLeaveRequestStatusService(
                new TestLeaveRequestStatusService(),
                guard);

        await service.ChangeStatusAsync(
            new ChangeLeaveRequestStatusRequest(
                Guid.NewGuid(),
                targetStatus));

        Assert.Equal(
            PermissionCodes.LeaveApprove,
            Assert.Single(
                guard.PermissionCodes));
    }

    [Fact]
    public async Task
        Cancel_RequiresLeaveCancel()
    {
        var guard =
            new TestAuthorizationGuard();

        var service =
            new AuthorizedLeaveRequestStatusService(
                new TestLeaveRequestStatusService(),
                guard);

        await service.ChangeStatusAsync(
            new ChangeLeaveRequestStatusRequest(
                Guid.NewGuid(),
                LeaveRequestStatus.Cancelled));

        Assert.Equal(
            PermissionCodes.LeaveCancel,
            Assert.Single(
                guard.PermissionCodes));
    }

    [Fact]
    public async Task
        Workspace_RequiresAttendanceAndLeaveView()
    {
        var guard =
            new TestAuthorizationGuard();

        var inner =
            new TestAttendanceLeaveWorkspaceQueryService();

        var service =
            new AuthorizedAttendanceLeaveWorkspaceQueryService(
                inner,
                guard);

        await service.GetAsync(
            default!);

        Assert.Equal(
            new[]
            {
                PermissionCodes.AttendanceView,
                PermissionCodes.LeaveView
            },
            guard.PermissionCodes);

        Assert.True(
            inner.GetCalled);
    }

    [Fact]
    public async Task
        Workspace_WhenDenied_DoesNotInvokeInner()
    {
        var guard =
            new TestAuthorizationGuard(
                denyPermissionCode:
                    PermissionCodes.LeaveView);

        var inner =
            new TestAttendanceLeaveWorkspaceQueryService();

        var service =
            new AuthorizedAttendanceLeaveWorkspaceQueryService(
                inner,
                guard);

        await Assert.ThrowsAsync<
            AuthorizationDeniedException>(
                () =>
                    service.GetAsync(
                        default!));

        Assert.False(
            inner.GetCalled);
    }

    private sealed class TestAuthorizationGuard
        : IAuthorizationGuard
    {
        private readonly string?
            _denyPermissionCode;

        public TestAuthorizationGuard(
            string? denyPermissionCode = null)
        {
            _denyPermissionCode =
                denyPermissionCode;
        }

        public List<string> PermissionCodes
        {
            get;
        } = [];

        public Task RequirePermissionAsync(
            string permissionCode,
            CancellationToken cancellationToken = default)
        {
            cancellationToken
                .ThrowIfCancellationRequested();

            PermissionCodes.Add(
                permissionCode);

            if (string.Equals(
                    permissionCode,
                    _denyPermissionCode,
                    StringComparison.Ordinal))
            {
                throw new AuthorizationDeniedException(
                    permissionCode);
            }

            return Task.CompletedTask;
        }
    }

    private sealed class TestLeaveRequestSubmissionService
        : ILeaveRequestSubmissionService
    {
        public bool Called
        {
            get;
            private set;
        }

        public Task<SubmitLeaveRequestResult> SubmitAsync(
            SubmitLeaveRequestRequest request,
            CancellationToken cancellationToken = default)
        {
            Called =
                true;

            return Task.FromResult(
                default(SubmitLeaveRequestResult)!);
        }
    }

    private sealed class TestLeaveRequestStatusService
        : ILeaveRequestStatusService
    {
        public Task<ChangeLeaveRequestStatusResult>
            ChangeStatusAsync(
                ChangeLeaveRequestStatusRequest request,
                CancellationToken cancellationToken = default)
        {
            return Task.FromResult(
                default(ChangeLeaveRequestStatusResult)!);
        }
    }

    private sealed class TestAttendanceLeaveWorkspaceQueryService
        : IAttendanceLeaveWorkspaceQueryService
    {
        public bool GetCalled
        {
            get;
            private set;
        }

        public Task<IReadOnlyList<AttendanceLeaveEmployeeItem>>
            GetEmployeesAsync(
                CancellationToken cancellationToken = default)
        {
            return Task.FromResult<
                IReadOnlyList<AttendanceLeaveEmployeeItem>>(
                    []);
        }

        public Task<IReadOnlyList<LeaveTypeWorkspaceOption>>
            GetActiveLeaveTypesAsync(
                CancellationToken cancellationToken = default)
        {
            return Task.FromResult<
                IReadOnlyList<LeaveTypeWorkspaceOption>>(
                    []);
        }

        public Task<AttendanceLeaveWorkspaceSnapshot> GetAsync(
            AttendanceLeaveWorkspaceQuery query,
            CancellationToken cancellationToken = default)
        {
            GetCalled =
                true;

            return Task.FromResult(
                default(AttendanceLeaveWorkspaceSnapshot)!);
        }
    }
}
