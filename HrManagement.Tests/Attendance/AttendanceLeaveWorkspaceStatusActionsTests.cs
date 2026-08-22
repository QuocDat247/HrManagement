using HrManagement.Application.Attendance.Calculations;
using HrManagement.Application.Attendance.Records;
using HrManagement.Application.Leave.Requests;
using HrManagement.Application.Workspaces.AttendanceLeave;
using HrManagement.Desktop.ViewModels;
using HrManagement.Domain.Leave.Requests;
using static HrManagement.Tests.Attendance.AttendanceLeaveWorkspaceViewModelTests;

namespace HrManagement.Tests.Attendance;

public sealed class AttendanceLeaveWorkspaceStatusActionsTests
{
    [Fact]
    public async Task PendingRequest_EnablesApproveRejectAndCancel()
    {
        TestContext test =
            await CreateLoadedContextAsync(
                LeaveRequestStatus.Pending);

        Assert.True(
            test.ViewModel
                .ApproveLeaveRequestCommand
                .CanExecute(null));

        Assert.True(
            test.ViewModel
                .RejectLeaveRequestCommand
                .CanExecute(null));

        Assert.True(
            test.ViewModel
                .CancelLeaveRequestCommand
                .CanExecute(null));
    }

    [Fact]
    public async Task Approve_MapsRequestNoteAndRefreshes()
    {
        TestContext test =
            await CreateLoadedContextAsync(
                LeaveRequestStatus.Pending);

        test.ViewModel.StatusChangeNote =
            "Đã kiểm tra";

        await test.ViewModel
            .ApproveLeaveRequestCommand
            .ExecuteAsync(null);

        ChangeLeaveRequestStatusRequest request =
            Assert.IsType<ChangeLeaveRequestStatusRequest>(
                test.StatusService.Request);

        Assert.Equal(
            test.LeaveRequestId,
            request.LeaveRequestId);

        Assert.Equal(
            LeaveRequestStatus.Approved,
            request.TargetStatus);

        Assert.Equal(
            "Đã kiểm tra",
            request.Note);

        Assert.Equal(
            "Đã duyệt đơn nghỉ phép.",
            test.ViewModel.OperationMessage);

        Assert.Null(
            test.ViewModel.ErrorMessage);

        Assert.Null(
            test.ViewModel.StatusChangeNote);

        Assert.Equal(
            2,
            test.QueryService.GetCallCount);
    }

    [Fact]
    public async Task Reject_MapsRejectedStatusAndRefreshes()
    {
        TestContext test =
            await CreateLoadedContextAsync(
                LeaveRequestStatus.Pending);

        test.ViewModel.StatusChangeNote =
            "Không đủ điều kiện";

        await test.ViewModel
            .RejectLeaveRequestCommand
            .ExecuteAsync(null);

        Assert.Equal(
            LeaveRequestStatus.Rejected,
            test.StatusService.Request!
                .TargetStatus);

        Assert.Equal(
            "Không đủ điều kiện",
            test.StatusService.Request.Note);

        Assert.Equal(
            "Đã từ chối đơn nghỉ phép.",
            test.ViewModel.OperationMessage);

        Assert.Equal(
            2,
            test.QueryService.GetCallCount);
    }

    [Fact]
    public async Task ApprovedRequest_AllowsOnlyCancelAndMapsCancellation()
    {
        TestContext test =
            await CreateLoadedContextAsync(
                LeaveRequestStatus.Approved);

        Assert.False(
            test.ViewModel
                .ApproveLeaveRequestCommand
                .CanExecute(null));

        Assert.False(
            test.ViewModel
                .RejectLeaveRequestCommand
                .CanExecute(null));

        Assert.True(
            test.ViewModel
                .CancelLeaveRequestCommand
                .CanExecute(null));

        await test.ViewModel
            .CancelLeaveRequestCommand
            .ExecuteAsync(null);

        Assert.Equal(
            LeaveRequestStatus.Cancelled,
            test.StatusService.Request!
                .TargetStatus);

        Assert.Equal(
            "Đã hủy đơn nghỉ phép.",
            test.ViewModel.OperationMessage);
    }

    [Fact]
    public async Task TerminalRequests_DisableAllStatusActions()
    {
        TestContext rejected =
            await CreateLoadedContextAsync(
                LeaveRequestStatus.Rejected);

        Assert.False(
            rejected.ViewModel
                .ApproveLeaveRequestCommand
                .CanExecute(null));

        Assert.False(
            rejected.ViewModel
                .RejectLeaveRequestCommand
                .CanExecute(null));

        Assert.False(
            rejected.ViewModel
                .CancelLeaveRequestCommand
                .CanExecute(null));

        TestContext cancelled =
            await CreateLoadedContextAsync(
                LeaveRequestStatus.Cancelled);

        Assert.False(
            cancelled.ViewModel
                .ApproveLeaveRequestCommand
                .CanExecute(null));

        Assert.False(
            cancelled.ViewModel
                .RejectLeaveRequestCommand
                .CanExecute(null));

        Assert.False(
            cancelled.ViewModel
                .CancelLeaveRequestCommand
                .CanExecute(null));
    }

    [Fact]
    public async Task StatusFailure_ShowsBusinessErrorAndDoesNotRefresh()
    {
        TestContext test =
            await CreateLoadedContextAsync(
                LeaveRequestStatus.Pending);

        test.StatusService.ResultOverride =
            new ChangeLeaveRequestStatusResult(
                IsSuccessful: false,
                ErrorMessage:
                    "Trạng thái đơn đã thay đổi.");

        await test.ViewModel
            .ApproveLeaveRequestCommand
            .ExecuteAsync(null);

        Assert.Equal(
            1,
            test.StatusService.CallCount);

        Assert.Equal(
            "Trạng thái đơn đã thay đổi.",
            test.ViewModel.ErrorMessage);

        Assert.Null(
            test.ViewModel.OperationMessage);

        Assert.Equal(
            1,
            test.QueryService.GetCallCount);
    }

    private static async Task<TestContext>
        CreateLoadedContextAsync(
            LeaveRequestStatus status)
    {
        Guid leaveRequestId =
            Guid.NewGuid();

        Guid employeeId =
            Guid.NewGuid();

        var queryService =
            new StatusActionWorkspaceQueryService(
                new LeaveWorkspaceItem(
                    leaveRequestId,
                    employeeId,
                    "EMP-A",
                    "Nhân viên A",
                    Guid.NewGuid(),
                    "ANNUAL",
                    "Nghỉ phép năm",
                    true,
                    new DateOnly(
                        2026,
                        8,
                        21),
                    new DateOnly(
                        2026,
                        8,
                        22),
                    status,
                    new DateTime(
                        2026,
                        8,
                        20,
                        4,
                        0,
                        0,
                        DateTimeKind.Utc),
                    "Kiểm thử"));

        var statusService =
            new TestLeaveRequestStatusService();

        var viewModel =
            new AttendanceLeaveWorkspaceViewModel(
                queryService,
                new StubAttendancePunchService(),
                new StubAttendanceRecalculationService(),
                new StatusActionLeaveSubmissionService(),
                statusService,
                new NoOpDailyAttendanceGenerationService(),
                new StatusActionTimeProvider(
                    new DateTimeOffset(
                        2026,
                        8,
                        21,
                        7,
                        0,
                        0,
                        TimeSpan.Zero)));

        await viewModel.LoadAsync();

        viewModel.SelectedLeaveRequestItem =
            Assert.Single(
                viewModel.LeaveRequestItems);

        return new TestContext(
            viewModel,
            queryService,
            statusService,
            leaveRequestId);
    }

    private sealed record TestContext(
        AttendanceLeaveWorkspaceViewModel ViewModel,
        StatusActionWorkspaceQueryService QueryService,
        TestLeaveRequestStatusService StatusService,
        Guid LeaveRequestId);

    private sealed class StatusActionWorkspaceQueryService
        : IAttendanceLeaveWorkspaceQueryService
    {
        private readonly LeaveWorkspaceItem
            _leaveRequest;

        public StatusActionWorkspaceQueryService(
            LeaveWorkspaceItem leaveRequest)
        {
            _leaveRequest =
                leaveRequest;
        }

        public int GetCallCount
        {
            get;
            private set;
        }

        public Task<IReadOnlyList<AttendanceLeaveEmployeeItem>>
            GetEmployeesAsync(
                CancellationToken cancellationToken = default)
        {
            IReadOnlyList<AttendanceLeaveEmployeeItem> result =
                [];

            return Task.FromResult(
                result);
        }

        public Task<IReadOnlyList<LeaveTypeWorkspaceOption>>
            GetActiveLeaveTypesAsync(
                CancellationToken cancellationToken = default)
        {
            IReadOnlyList<LeaveTypeWorkspaceOption> result =
                [];

            return Task.FromResult(
                result);
        }

        public Task<AttendanceLeaveWorkspaceSnapshot>
            GetAsync(
                AttendanceLeaveWorkspaceQuery query,
                CancellationToken cancellationToken = default)
        {
            GetCallCount++;

            return Task.FromResult(
                new AttendanceLeaveWorkspaceSnapshot(
                    [],
                    [
                        _leaveRequest
                    ]));
        }
    }

    private sealed class StatusActionLeaveSubmissionService
        : ILeaveRequestSubmissionService
    {
        public Task<SubmitLeaveRequestResult> SubmitAsync(
            SubmitLeaveRequestRequest request,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(
                new SubmitLeaveRequestResult(
                    IsSuccessful: true,
                    LeaveRequestId:
                        Guid.NewGuid(),
                    Status:
                        LeaveRequestStatus.Pending));
        }
    }

    private sealed class StatusActionTimeProvider
        : TimeProvider
    {
        private readonly DateTimeOffset
            _utcNow;

        public StatusActionTimeProvider(
            DateTimeOffset utcNow)
        {
            _utcNow =
                utcNow;
        }

        public override DateTimeOffset GetUtcNow()
        {
            return _utcNow;
        }

        public override TimeZoneInfo LocalTimeZone =>
            TimeZoneInfo.Utc;
    }
}

internal sealed class TestLeaveRequestStatusService
    : ILeaveRequestStatusService
{
    public ChangeLeaveRequestStatusRequest? Request
    {
        get;
        private set;
    }

    public int CallCount
    {
        get;
        private set;
    }

    public ChangeLeaveRequestStatusResult? ResultOverride
    {
        get;
        set;
    }

    public Task<ChangeLeaveRequestStatusResult>
        ChangeStatusAsync(
            ChangeLeaveRequestStatusRequest request,
            CancellationToken cancellationToken = default)
    {
        CallCount++;

        Request =
            request;

        return Task.FromResult(
            ResultOverride
            ?? new ChangeLeaveRequestStatusResult(
                IsSuccessful: true,
                LeaveRequestId:
                    request.LeaveRequestId,
                Status:
                    request.TargetStatus,
                StatusChangeId:
                    Guid.NewGuid()));
    }

    internal sealed class StubAttendancePunchService
    : IAttendancePunchService
    {
        public RecordAttendancePunchRequest? Request
        {
            get;
            private set;
        }

        public RecordAttendancePunchResult Result
        {
            get;
            set;
        } =
            new(
                IsSuccessful: true,
                AttendanceRecordId:
                    Guid.NewGuid(),
                AttendanceEventId:
                    Guid.NewGuid(),
                WorkDate:
                    new DateOnly(
                        2026,
                        8,
                        21));

        public Task<RecordAttendancePunchResult> RecordAsync(
            RecordAttendancePunchRequest request,
            CancellationToken cancellationToken = default)
        {
            Request =
                request;

            return Task.FromResult(
                Result);
        }
    }

    internal sealed class StubAttendanceRecalculationService
        : IAttendanceRecalculationService
    {
        public RecalculateAttendanceRequest? Request
        {
            get;
            private set;
        }

        public RecalculateAttendanceResult Result
        {
            get;
            set;
        } =
            new(
                IsSuccessful: true);

        public Task<RecalculateAttendanceResult> RecalculateAsync(
            RecalculateAttendanceRequest request,
            CancellationToken cancellationToken = default)
        {
            Request =
                request;

            return Task.FromResult(
                Result);
        }
    }
}
