using HrManagement.Application.Attendance.Calculations;
using HrManagement.Application.Attendance.Corrections;
using HrManagement.Domain.Attendance.Corrections;
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

    private static AttendanceWorkspaceItem CreateAttendanceItem(
    Guid attendanceRecordId,
    Guid employeeId)
    {
        return new AttendanceWorkspaceItem(
            attendanceRecordId,
            employeeId,
            "EMP006",
            "Nguyễn Chí Thành",
            new DateOnly(
                2026,
                8,
                21),
            true,
            new TimeOnly(
                8,
                0),
            new TimeOnly(
                17,
                0),
            AttendanceCalculationStatus.Present,
            480,
            0,
            0);
    }

    [Fact]
    public async Task AddCorrection_MapsEditorValuesRecalculatesAndRefreshes()
    {
        TestContext test =
            CreateContext();

        Guid attendanceRecordId =
            Guid.NewGuid();

        AttendanceWorkspaceItem attendance =
            CreateAttendanceItem(
                attendanceRecordId,
                test.EmployeeId);

        test.QueryService.Attendance =
        [
            attendance
        ];

        test.CorrectionWorkspaceQueryService.Snapshot =
            new AttendanceCorrectionWorkspaceSnapshot(
                attendanceRecordId,
                test.EmployeeId,
                attendance.WorkDate,
                "UTC",
                [],
                []);

        await test.ViewModel.LoadAsync();

        test.ViewModel.IsLoading =
            true;

        test.ViewModel.SelectedAttendanceItem =
            attendance;

        test.ViewModel.IsLoading =
            false;

        await test.ViewModel
            .LoadCorrectionWorkspaceCommand
            .ExecuteAsync(null);

        test.ViewModel.CorrectionEventType =
            AttendanceEventType.ClockIn;

        test.ViewModel.CorrectionDate =
            new DateTime(
                2026,
                8,
                21);

        test.ViewModel.CorrectionTimeText =
            "09:15";

        test.ViewModel.CorrectionReason =
            "Bổ sung giờ vào ca";

        await test.ViewModel
            .AddAttendanceCorrectionCommand
            .ExecuteAsync(null);

        ApplyAttendanceCorrectionRequest request =
            Assert.IsType<ApplyAttendanceCorrectionRequest>(
                test.CorrectionService.Request);

        Assert.Equal(
            attendanceRecordId,
            request.AttendanceRecordId);

        Assert.Equal(
            AttendanceCorrectionKind.AddEvent,
            request.Kind);

        Assert.Null(
            request.AffectedEventId);

        Assert.Equal(
            AttendanceEventType.ClockIn,
            request.AfterEventType);

        Assert.Equal(
            new DateTime(
                2026,
                8,
                21,
                9,
                15,
                0,
                DateTimeKind.Utc),
            request.AfterOccurredAtUtc);

        Assert.Equal(
            "Bổ sung giờ vào ca",
            request.Reason);

        Assert.Equal(
            attendanceRecordId,
            test.RecalculationService
                .Request!
                .AttendanceRecordId);

        Assert.Equal(
            "Đã bổ sung sự kiện chấm công.",
            test.ViewModel.OperationMessage);

        Assert.Null(
            test.ViewModel.CorrectionReason);
    }

    [Fact]
    public async Task ChangeCorrection_MapsSelectedEffectiveEvent()
    {
        TestContext test =
            CreateContext();

        Guid attendanceRecordId =
            Guid.NewGuid();

        Guid eventId =
            Guid.NewGuid();

        AttendanceWorkspaceItem attendance =
            CreateAttendanceItem(
                attendanceRecordId,
                test.EmployeeId);

        var effectiveEvent =
            new AttendanceCorrectionWorkspaceEventItem(
                eventId,
                AttendanceEventType.ClockIn,
                new DateTime(
                    2026,
                    8,
                    21,
                    8,
                    0,
                    0,
                    DateTimeKind.Utc),
                new DateTime(
                    2026,
                    8,
                    21,
                    8,
                    0,
                    0,
                    DateTimeKind.Unspecified),
                false,
                false,
                null);

        test.QueryService.Attendance =
        [
            attendance
        ];

        test.CorrectionWorkspaceQueryService.Snapshot =
            new AttendanceCorrectionWorkspaceSnapshot(
                attendanceRecordId,
                test.EmployeeId,
                attendance.WorkDate,
                "UTC",
                [
                    effectiveEvent
                ],
                []);

        await test.ViewModel.LoadAsync();

        test.ViewModel.IsLoading =
            true;

        test.ViewModel.SelectedAttendanceItem =
            attendance;

        test.ViewModel.IsLoading =
            false;

        await test.ViewModel
            .LoadCorrectionWorkspaceCommand
            .ExecuteAsync(null);

        test.ViewModel.SelectedCorrectionEvent =
            effectiveEvent;

        test.ViewModel.CorrectionTimeText =
            "08:30";

        test.ViewModel.CorrectionReason =
            "Điều chỉnh giờ vào ca";

        await test.ViewModel
            .ChangeAttendanceCorrectionCommand
            .ExecuteAsync(null);

        ApplyAttendanceCorrectionRequest request =
            Assert.IsType<ApplyAttendanceCorrectionRequest>(
                test.CorrectionService.Request);

        Assert.Equal(
            AttendanceCorrectionKind.ChangeEvent,
            request.Kind);

        Assert.Equal(
            eventId,
            request.AffectedEventId);

        Assert.Equal(
            AttendanceEventType.ClockIn,
            request.AfterEventType);

        Assert.Equal(
            new DateTime(
                2026,
                8,
                21,
                8,
                30,
                0,
                DateTimeKind.Utc),
            request.AfterOccurredAtUtc);

        Assert.Equal(
            attendanceRecordId,
            test.RecalculationService
                .Request!
                .AttendanceRecordId);

        Assert.Equal(
            "Đã sửa sự kiện chấm công.",
            test.ViewModel.OperationMessage);
    }

    [Fact]
    public async Task VoidCorrection_SendsNoAfterStateAndRecalculates()
    {
        TestContext test =
            CreateContext();

        Guid attendanceRecordId =
            Guid.NewGuid();

        Guid eventId =
            Guid.NewGuid();

        AttendanceWorkspaceItem attendance =
            CreateAttendanceItem(
                attendanceRecordId,
                test.EmployeeId);

        var effectiveEvent =
            new AttendanceCorrectionWorkspaceEventItem(
                eventId,
                AttendanceEventType.ClockOut,
                new DateTime(
                    2026,
                    8,
                    21,
                    17,
                    0,
                    0,
                    DateTimeKind.Utc),
                new DateTime(
                    2026,
                    8,
                    21,
                    17,
                    0,
                    0,
                    DateTimeKind.Unspecified),
                false,
                false,
                null);

        test.QueryService.Attendance =
        [
            attendance
        ];

        test.CorrectionWorkspaceQueryService.Snapshot =
            new AttendanceCorrectionWorkspaceSnapshot(
                attendanceRecordId,
                test.EmployeeId,
                attendance.WorkDate,
                "UTC",
                [
                    effectiveEvent
                ],
                []);

        await test.ViewModel.LoadAsync();

        test.ViewModel.IsLoading =
            true;

        test.ViewModel.SelectedAttendanceItem =
            attendance;

        test.ViewModel.IsLoading =
            false;

        await test.ViewModel
            .LoadCorrectionWorkspaceCommand
            .ExecuteAsync(null);

        test.ViewModel.SelectedCorrectionEvent =
            effectiveEvent;

        test.ViewModel.CorrectionReason =
            "Hủy sự kiện nhập nhầm";

        await test.ViewModel
            .VoidAttendanceCorrectionCommand
            .ExecuteAsync(null);

        ApplyAttendanceCorrectionRequest request =
            Assert.IsType<ApplyAttendanceCorrectionRequest>(
                test.CorrectionService.Request);

        Assert.Equal(
            AttendanceCorrectionKind.VoidEvent,
            request.Kind);

        Assert.Equal(
            eventId,
            request.AffectedEventId);

        Assert.Null(
            request.AfterEventType);

        Assert.Null(
            request.AfterOccurredAtUtc);

        Assert.Equal(
            "Hủy sự kiện nhập nhầm",
            request.Reason);

        Assert.Equal(
            attendanceRecordId,
            test.RecalculationService
                .Request!
                .AttendanceRecordId);

        Assert.Equal(
            "Đã hủy hiệu lực sự kiện chấm công.",
            test.ViewModel.OperationMessage);
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

        var correctionService =
            new TestAttendanceCorrectionService();

        var correctionWorkspaceQueryService =
            new TestAttendanceCorrectionWorkspaceQueryService();

        var viewModel =
            new AttendanceLeaveWorkspaceViewModel(
                queryService,
                punchService,
                recalculationService,
                new TestLeaveSubmissionService(),
                new TestLeaveStatusService(),
                generationService,
                correctionService,
                correctionWorkspaceQueryService,
                new FixedTimeProvider(
                    new DateTimeOffset(
                        FixedUtc)));

        return new TestContext(
            viewModel,
            queryService,
            punchService,
            recalculationService,
            generationService,
            correctionService,
            correctionWorkspaceQueryService,
            employeeId);
    }

    private sealed record TestContext(
        AttendanceLeaveWorkspaceViewModel ViewModel,
        TestWorkspaceQueryService QueryService,
        TestAttendancePunchService PunchService,
        TestAttendanceRecalculationService RecalculationService,
        TestDailyAttendanceGenerationService GenerationService,
        TestAttendanceCorrectionService CorrectionService,
        TestAttendanceCorrectionWorkspaceQueryService
            CorrectionWorkspaceQueryService,
        Guid EmployeeId);

    private sealed class TestWorkspaceQueryService
        : IAttendanceLeaveWorkspaceQueryService
    {
        public IReadOnlyList<AttendanceWorkspaceItem>
            Attendance
        {
            get;
            set;
        } = [];

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
                    Attendance,
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

    private sealed class TestAttendanceCorrectionService
    : IAttendanceCorrectionService
    {
        public ApplyAttendanceCorrectionRequest? Request
        {
            get;
            private set;
        }

        public ApplyAttendanceCorrectionResult Result
        {
            get;
            set;
        } =
            new(
                IsSuccessful: true,
                AttendanceCorrectionId:
                    Guid.NewGuid(),
                Revision:
                    1);

        public Task<ApplyAttendanceCorrectionResult> ApplyAsync(
            ApplyAttendanceCorrectionRequest request,
            CancellationToken cancellationToken = default)
        {
            Request =
                request;

            return Task.FromResult(
                Result);
        }
    }

    private sealed class TestAttendanceCorrectionWorkspaceQueryService
        : IAttendanceCorrectionWorkspaceQueryService
    {
        public AttendanceCorrectionWorkspaceSnapshot? Snapshot
        {
            get;
            set;
        }

        public Guid? AttendanceRecordId
        {
            get;
            private set;
        }

        public int CallCount
        {
            get;
            private set;
        }

        public Task<AttendanceCorrectionWorkspaceSnapshot?> GetAsync(
            Guid attendanceRecordId,
            CancellationToken cancellationToken = default)
        {
            AttendanceRecordId =
                attendanceRecordId;

            CallCount++;

            return Task.FromResult(
                Snapshot);
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
