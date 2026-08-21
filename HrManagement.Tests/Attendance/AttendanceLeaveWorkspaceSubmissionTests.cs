using HrManagement.Application.Attendance.Calculations;
using HrManagement.Application.Attendance.Records;
using HrManagement.Application.Leave.Requests;
using HrManagement.Application.Workspaces.AttendanceLeave;
using HrManagement.Desktop.ViewModels;
using HrManagement.Domain.Leave.Requests;
using static HrManagement.Tests.Attendance.AttendanceLeaveWorkspaceViewModelTests;

namespace HrManagement.Tests.Attendance;

public sealed class AttendanceLeaveWorkspaceSubmissionTests
{
    [Fact]
    public async Task Submit_UsesSelectedEmployeeTypeDatesAndReason()
    {
        TestContext test =
            CreateContext();

        await test.ViewModel.LoadAsync();

        test.ViewModel.RequestStartDate =
            new DateTime(
                2026,
                8,
                25);

        test.ViewModel.RequestEndDate =
            new DateTime(
                2026,
                8,
                26);

        test.ViewModel.RequestReason =
            "Nghỉ phép";

        await test.ViewModel
            .SubmitLeaveRequestCommand
            .ExecuteAsync(null);

        SubmitLeaveRequestRequest request =
            Assert.IsType<SubmitLeaveRequestRequest>(
                test.SubmissionService.Request);

        Assert.Equal(
            test.EmployeeId,
            request.EmployeeId);

        Assert.Equal(
            test.LeaveTypeId,
            request.LeaveTypeId);

        Assert.Equal(
            new DateOnly(
                2026,
                8,
                25),
            request.StartDate);

        Assert.Equal(
            new DateOnly(
                2026,
                8,
                26),
            request.EndDate);

        Assert.Equal(
            "Nghỉ phép",
            request.Reason);

        Assert.Equal(
            "Đã gửi đơn nghỉ phép.",
            test.ViewModel.OperationMessage);

        Assert.Null(
            test.ViewModel.ErrorMessage);

        Assert.Equal(
            2,
            test.QueryService.GetCallCount);
    }

    [Fact]
    public async Task SubmitWithoutEmployee_DoesNotCallService()
    {
        TestContext test =
            CreateContext();

        await test.ViewModel.LoadAsync();

        test.ViewModel.SelectedRequestEmployee =
            null;

        await test.ViewModel
            .SubmitLeaveRequestCommand
            .ExecuteAsync(null);

        Assert.Equal(
            0,
            test.SubmissionService.CallCount);

        Assert.Equal(
            "Vui lòng chọn nhân viên.",
            test.ViewModel.ErrorMessage);
    }

    [Fact]
    public async Task SubmitWithInvalidDateRange_DoesNotCallService()
    {
        TestContext test =
            CreateContext();

        await test.ViewModel.LoadAsync();

        test.ViewModel.RequestStartDate =
            new DateTime(
                2026,
                8,
                26);

        test.ViewModel.RequestEndDate =
            new DateTime(
                2026,
                8,
                25);

        await test.ViewModel
            .SubmitLeaveRequestCommand
            .ExecuteAsync(null);

        Assert.Equal(
            0,
            test.SubmissionService.CallCount);

        Assert.Equal(
            "Ngày kết thúc nghỉ không thể trước ngày bắt đầu nghỉ.",
            test.ViewModel.ErrorMessage);
    }

    [Fact]
    public async Task SubmissionFailure_ShowsBusinessErrorAndDoesNotRefresh()
    {
        TestContext test =
            CreateContext();

        test.SubmissionService.Result =
            new SubmitLeaveRequestResult(
                IsSuccessful: false,
                ErrorMessage:
                    "Khoảng nghỉ phép bị trùng.");

        await test.ViewModel.LoadAsync();

        await test.ViewModel
            .SubmitLeaveRequestCommand
            .ExecuteAsync(null);

        Assert.Equal(
            1,
            test.SubmissionService.CallCount);

        Assert.Equal(
            "Khoảng nghỉ phép bị trùng.",
            test.ViewModel.ErrorMessage);

        Assert.Null(
            test.ViewModel.OperationMessage);

        Assert.Equal(
            1,
            test.QueryService.GetCallCount);
    }

    private static TestContext CreateContext()
    {
        Guid employeeId =
            Guid.NewGuid();

        Guid leaveTypeId =
            Guid.NewGuid();

        var queryService =
            new StubWorkspaceQueryService
            {
                Employees =
                [
                    new AttendanceLeaveEmployeeItem(
                        employeeId,
                        "EMP-A",
                        "Nhân viên A")
                ],

                LeaveTypes =
                [
                    new LeaveTypeWorkspaceOption(
                        leaveTypeId,
                        "ANNUAL",
                        "Nghỉ phép năm",
                        true)
                ]
            };

        var submissionService =
            new StubLeaveRequestSubmissionService();

        var viewModel =
            new AttendanceLeaveWorkspaceViewModel(
                queryService,
                new StubAttendancePunchService(),
                new StubAttendanceRecalculationService(),
                submissionService,
                new TestLeaveRequestStatusService(),
                new FixedTimeProvider(
                    new DateTimeOffset(
                        2026,
                        8,
                        21,
                        7,
                        0,
                        0,
                        TimeSpan.Zero)));

        return new TestContext(
            viewModel,
            queryService,
            submissionService,
            employeeId,
            leaveTypeId);
    }

    private sealed record TestContext(
        AttendanceLeaveWorkspaceViewModel ViewModel,
        StubWorkspaceQueryService QueryService,
        StubLeaveRequestSubmissionService SubmissionService,
        Guid EmployeeId,
        Guid LeaveTypeId);

    private sealed class StubWorkspaceQueryService
        : IAttendanceLeaveWorkspaceQueryService
    {
        public IReadOnlyList<AttendanceLeaveEmployeeItem>
            Employees
        {
            get;
            set;
        } = [];

        public IReadOnlyList<LeaveTypeWorkspaceOption>
            LeaveTypes
        {
            get;
            set;
        } = [];

        public int GetCallCount
        {
            get;
            private set;
        }

        public Task<IReadOnlyList<AttendanceLeaveEmployeeItem>>
            GetEmployeesAsync(
                CancellationToken cancellationToken = default)
        {
            return Task.FromResult(
                Employees);
        }

        public Task<IReadOnlyList<LeaveTypeWorkspaceOption>>
            GetActiveLeaveTypesAsync(
                CancellationToken cancellationToken = default)
        {
            return Task.FromResult(
                LeaveTypes);
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
                    []));
        }
    }

    private sealed class StubLeaveRequestSubmissionService
        : ILeaveRequestSubmissionService
    {
        public SubmitLeaveRequestRequest? Request
        {
            get;
            private set;
        }

        public int CallCount
        {
            get;
            private set;
        }

        public SubmitLeaveRequestResult Result
        {
            get;
            set;
        } =
            new(
                IsSuccessful: true,
                LeaveRequestId:
                    Guid.NewGuid(),
                Status:
                    LeaveRequestStatus.Pending);

        public Task<SubmitLeaveRequestResult> SubmitAsync(
            SubmitLeaveRequestRequest request,
            CancellationToken cancellationToken = default)
        {
            CallCount++;

            Request =
                request;

            return Task.FromResult(
                Result);
        }
    }

    private sealed class FixedTimeProvider
        : TimeProvider
    {
        private readonly DateTimeOffset
            _utcNow;

        public FixedTimeProvider(
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
