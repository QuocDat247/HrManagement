using HrManagement.Application.Attendance.Calculations;
using HrManagement.Application.Attendance.Generation;
using HrManagement.Application.Attendance.Records;
using HrManagement.Application.Leave.Requests;
using HrManagement.Application.Workspaces.AttendanceLeave;
using HrManagement.Desktop.ViewModels;
using HrManagement.Domain.Attendance.Calculations;
using HrManagement.Domain.Attendance.Records;
using HrManagement.Domain.Leave.Requests;

namespace HrManagement.Tests.Attendance;

public sealed class AttendanceLeaveWorkspaceAttendanceActionsTests
{
    [Fact]
    public async Task EmployeeSearch_FiltersByEmployeeCode()
    {
        TestContext test =
            CreateContext();

        await test.ViewModel.LoadAsync();

        test.ViewModel.EmployeeSearchText =
            "EMP006";

        Assert.Equal(
            2,
            test.ViewModel
                .FilteredEmployeeOptions
                .Count);

        Assert.Null(
            test.ViewModel
                .FilteredEmployeeOptions[0]
                .EmployeeId);

        AttendanceLeaveEmployeeFilterOption employee =
            test.ViewModel
                .FilteredEmployeeOptions[1];

        Assert.Equal(
            test.EmployeeId,
            employee.EmployeeId);

        Assert.Contains(
            "EMP006",
            employee.DisplayName);
    }

    [Fact]
    public async Task EmployeeSearch_FiltersByEmployeeName()
    {
        TestContext test =
            CreateContext();

        await test.ViewModel.LoadAsync();

        test.ViewModel.EmployeeSearchText =
            "chí thành";

        Assert.Equal(
            2,
            test.ViewModel
                .FilteredEmployeeOptions
                .Count);

        AttendanceLeaveEmployeeFilterOption employee =
            test.ViewModel
                .FilteredEmployeeOptions
                .Single(
                    option =>
                        option.EmployeeId.HasValue);

        Assert.Equal(
            test.EmployeeId,
            employee.EmployeeId);

        Assert.Contains(
            "Nguyễn Chí Thành",
            employee.DisplayName);
    }

    [Fact]
    public async Task ClockIn_RecordsUtcPunchRecalculatesAndRefreshes()
    {
        TestContext test =
            CreateContext();

        await test.ViewModel.LoadAsync();

        SelectEmployee(
            test);

        Guid attendanceRecordId =
            Guid.NewGuid();

        test.PunchService.Result =
            new RecordAttendancePunchResult(
                IsSuccessful: true,
                AttendanceRecordId:
                    attendanceRecordId,
                AttendanceEventId:
                    Guid.NewGuid(),
                WorkDate:
                    new DateOnly(
                        2026,
                        8,
                        21));

        await test.ViewModel
            .ClockInCommand
            .ExecuteAsync(null);

        RecordAttendancePunchRequest punchRequest =
            Assert.IsType<RecordAttendancePunchRequest>(
                test.PunchService.Request);

        Assert.Equal(
            test.EmployeeId,
            punchRequest.EmployeeId);

        Assert.Equal(
            AttendanceEventType.ClockIn,
            punchRequest.EventType);

        Assert.Equal(
            FixedUtc,
            punchRequest.OccurredAtUtc);

        Assert.Equal(
            DateTimeKind.Utc,
            punchRequest.OccurredAtUtc.Kind);

        RecalculateAttendanceRequest recalculationRequest =
            Assert.IsType<RecalculateAttendanceRequest>(
                test.RecalculationService.Request);

        Assert.Equal(
            attendanceRecordId,
            recalculationRequest.AttendanceRecordId);

        Assert.Equal(
            "Đã ghi nhận vào ca.",
            test.ViewModel.OperationMessage);

        Assert.Null(
            test.ViewModel.ErrorMessage);

        Assert.Equal(
            2,
            test.QueryService.GetCallCount);
    }

    [Fact]
    public async Task ClockOut_RecordsClockOut()
    {
        TestContext test =
            CreateContext();

        await test.ViewModel.LoadAsync();

        SelectEmployee(
            test);

        Guid attendanceRecordId =
            Guid.NewGuid();

        test.PunchService.Result =
            new RecordAttendancePunchResult(
                IsSuccessful: true,
                AttendanceRecordId:
                    attendanceRecordId,
                AttendanceEventId:
                    Guid.NewGuid(),
                WorkDate:
                    new DateOnly(
                        2026,
                        8,
                        21));

        await test.ViewModel
            .ClockOutCommand
            .ExecuteAsync(null);

        RecordAttendancePunchRequest punchRequest =
            Assert.IsType<RecordAttendancePunchRequest>(
                test.PunchService.Request);

        Assert.Equal(
            test.EmployeeId,
            punchRequest.EmployeeId);

        Assert.Equal(
            AttendanceEventType.ClockOut,
            punchRequest.EventType);

        Assert.Equal(
            FixedUtc,
            punchRequest.OccurredAtUtc);

        Assert.Equal(
            attendanceRecordId,
            test.RecalculationService
                .Request!
                .AttendanceRecordId);

        Assert.Equal(
            "Đã ghi nhận ra ca.",
            test.ViewModel.OperationMessage);

        Assert.Equal(
            2,
            test.QueryService.GetCallCount);
    }

    [Fact]
    public void GenerationDate_DefaultsToLocalToday()
    {
        TestContext test =
            CreateContext();

        Assert.Equal(
            new DateTime(
                2026,
                8,
                21),
            test.ViewModel.GenerationDate);
    }

    [Fact]
    public async Task GenerateAttendance_AllEmployeesMapsNullEmployee()
    {
        TestContext test =
            CreateContext();

        await test.ViewModel.LoadAsync();

        test.ViewModel.GenerationDate =
            new DateTime(
                2026,
                8,
                21);

        await test.ViewModel
            .GenerateAttendanceCommand
            .ExecuteAsync(null);

        GenerateDailyAttendanceRequest request =
            Assert.IsType<GenerateDailyAttendanceRequest>(
                test.GenerationService.Request);

        Assert.Equal(
            new DateOnly(
                2026,
                8,
                21),
            request.WorkDate);

        Assert.Null(
            request.EmployeeId);

        Assert.Equal(
            "Đã sinh 1 bản ghi chấm công.",
            test.ViewModel.OperationMessage);

        Assert.Equal(
            2,
            test.QueryService.GetCallCount);
    }

    [Fact]
    public async Task GenerateAttendance_SelectedEmployeeMapsEmployeeId()
    {
        TestContext test =
            CreateContext();

        await test.ViewModel.LoadAsync();

        test.ViewModel.SelectedEmployeeOption =
            test.ViewModel
                .EmployeeOptions
                .Single(
                    option =>
                        option.EmployeeId ==
                        test.EmployeeId);

        await test.ViewModel
            .GenerateAttendanceCommand
            .ExecuteAsync(null);

        GenerateDailyAttendanceRequest request =
            Assert.IsType<GenerateDailyAttendanceRequest>(
                test.GenerationService.Request);

        Assert.Equal(
            test.EmployeeId,
            request.EmployeeId);
    }

    [Fact]
    public async Task GenerateAttendance_BusinessFailureShowsError()
    {
        TestContext test =
            CreateContext();

        await test.ViewModel.LoadAsync();

        test.GenerationService.Result =
            new GenerateDailyAttendanceResult(
                false,
                ErrorMessage:
                    "Không tìm thấy phân lịch làm việc phù hợp.");

        await test.ViewModel
            .GenerateAttendanceCommand
            .ExecuteAsync(null);

        Assert.Equal(
            "Không tìm thấy phân lịch làm việc phù hợp.",
            test.ViewModel.ErrorMessage);

        Assert.Null(
            test.ViewModel.OperationMessage);

        Assert.Equal(
            1,
            test.QueryService.GetCallCount);
    }

    private static readonly DateTime FixedUtc =
        new(
            2026,
            8,
            21,
            8,
            30,
            0,
            DateTimeKind.Utc);

    private static void SelectEmployee(
        TestContext test)
    {
        test.ViewModel.SelectedEmployeeOption =
            test.ViewModel
                .EmployeeOptions
                .Single(
                    option =>
                        option.EmployeeId ==
                        test.EmployeeId);

        Assert.True(
            test.ViewModel
                .ClockInCommand
                .CanExecute(null));

        Assert.True(
            test.ViewModel
                .ClockOutCommand
                .CanExecute(null));
    }

    private static TestContext CreateContext()
    {
        Guid employeeId =
            Guid.NewGuid();

        var queryService =
            new TestWorkspaceQueryService
            {
                Employees =
                [
                    new AttendanceLeaveEmployeeItem(
                        employeeId,
                        "EMP006",
                        "Nguyễn Chí Thành"),

                    new AttendanceLeaveEmployeeItem(
                        Guid.NewGuid(),
                        "EMP007",
                        "Trần Minh Anh")
                ]
            };

        var generationService =
            new TestDailyAttendanceGenerationService();

        var punchService =
            new TestAttendancePunchService();

        var recalculationService =
            new TestAttendanceRecalculationService();

        var viewModel =
            new AttendanceLeaveWorkspaceViewModel(
                queryService,
                punchService,
                recalculationService,
                new TestLeaveSubmissionService(),
                new TestLeaveStatusService(),
                generationService,
                new FixedTimeProvider(
                    new DateTimeOffset(
                        FixedUtc)));

        return new TestContext(
            viewModel,
            queryService,
            punchService,
            recalculationService,
            generationService,
            employeeId);
    }

    private sealed record TestContext(
        AttendanceLeaveWorkspaceViewModel ViewModel,
        TestWorkspaceQueryService QueryService,
        TestAttendancePunchService PunchService,
        TestAttendanceRecalculationService RecalculationService,
        TestDailyAttendanceGenerationService GenerationService,
        Guid EmployeeId);

    private sealed class TestWorkspaceQueryService
        : IAttendanceLeaveWorkspaceQueryService
    {
        public IReadOnlyList<AttendanceLeaveEmployeeItem>
            Employees
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
                    []));
        }
    }

    private sealed class TestAttendancePunchService
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

    private sealed class TestAttendanceRecalculationService
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
                IsSuccessful: true,
                Status:
                    AttendanceCalculationStatus.Present);

        public Task<RecalculateAttendanceResult>
            RecalculateAsync(
                RecalculateAttendanceRequest request,
                CancellationToken cancellationToken = default)
        {
            Request =
                request;

            return Task.FromResult(
                Result);
        }
    }

    private sealed class TestLeaveSubmissionService
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

    private sealed class TestLeaveStatusService
        : ILeaveRequestStatusService
    {
        public Task<ChangeLeaveRequestStatusResult>
            ChangeStatusAsync(
                ChangeLeaveRequestStatusRequest request,
                CancellationToken cancellationToken = default)
        {
            return Task.FromResult(
                new ChangeLeaveRequestStatusResult(
                    IsSuccessful: true,
                    LeaveRequestId:
                        request.LeaveRequestId,
                    Status:
                        request.TargetStatus,
                    StatusChangeId:
                        Guid.NewGuid()));
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

    private sealed class TestDailyAttendanceGenerationService
    : IDailyAttendanceGenerationService
    {
        public GenerateDailyAttendanceRequest? Request
        {
            get;
            private set;
        }

        public GenerateDailyAttendanceResult Result
        {
            get;
            set;
        } =
            new(
                true,
                CreatedCount:
                    1);

        public Task<GenerateDailyAttendanceResult> GenerateAsync(
            GenerateDailyAttendanceRequest request,
            CancellationToken cancellationToken = default)
        {
            Request =
                request;

            return Task.FromResult(
                Result);
        }
    }
}
