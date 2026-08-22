using HrManagement.Application.Attendance.Calculations;
using HrManagement.Application.Attendance.Records;
using HrManagement.Application.Leave.Requests;
using HrManagement.Application.Workspaces.AttendanceLeave;
using HrManagement.Desktop.ViewModels;
using HrManagement.Domain.Attendance.Calculations;
using HrManagement.Domain.Leave.Requests;

namespace HrManagement.Tests.Attendance;

public sealed class AttendanceLeaveWorkspaceViewModelTests
{
    [Fact]
    public void Constructor_DefaultsToCurrentMonthAndAllEmployees()
    {
        var service =
            new StubWorkspaceQueryService();

        var viewModel =
            CreateViewModel(
                service);

        Assert.Equal(
            new DateTime(
                2026,
                8,
                1),
            viewModel.FromDate);

        Assert.Equal(
            new DateTime(
                2026,
                8,
                31),
            viewModel.ToDate);

        Assert.NotNull(
            viewModel.SelectedEmployeeOption);

        Assert.Null(
            viewModel
                .SelectedEmployeeOption!
                .EmployeeId);

        Assert.Single(
            viewModel.EmployeeOptions);
    }

    [Fact]
    public async Task LoadAsync_LoadsEmployeeOptionsAndSnapshot()
    {
        var service =
            new StubWorkspaceQueryService
            {
                Employees =
                [
                    new AttendanceLeaveEmployeeItem(
                        Guid.NewGuid(),
                        "EMP-A",
                        "Nhân viên A")
                ],

                Snapshot =
                    CreateSnapshot()
            };

        var viewModel =
            CreateViewModel(
                service);

        await viewModel.LoadAsync();

        Assert.Equal(
            2,
            viewModel.EmployeeOptions.Count);

        Assert.Equal(
            "Tất cả nhân viên",
            viewModel.EmployeeOptions[0].DisplayName);

        Assert.Equal(
            "EMP-A - Nhân viên A",
            viewModel.EmployeeOptions[1].DisplayName);

        Assert.Single(
            viewModel.AttendanceItems);

        Assert.Single(
            viewModel.LeaveRequestItems);

        Assert.Null(
            viewModel.ErrorMessage);

        Assert.False(
            viewModel.IsLoading);
    }

    [Fact]
    public async Task SelectedEmployee_IsPassedToWorkspaceQuery()
    {
        Guid employeeId =
            Guid.NewGuid();

        var service =
            new StubWorkspaceQueryService
            {
                Employees =
                [
                    new AttendanceLeaveEmployeeItem(
                        employeeId,
                        "EMP-A",
                        "Nhân viên A")
                ]
            };

        var viewModel =
            CreateViewModel(
                service);

        await viewModel.LoadAsync();

        viewModel.SelectedEmployeeOption =
            viewModel.EmployeeOptions
                .Single(
                    option =>
                        option.EmployeeId ==
                        employeeId);

        await viewModel.LoadAsync();

        Assert.NotNull(
            service.LastQuery);

        Assert.Equal(
            employeeId,
            service.LastQuery!.EmployeeId);
    }

    [Fact]
    public async Task InvalidDateRange_DoesNotQueryService()
    {
        var service =
            new StubWorkspaceQueryService();

        var viewModel =
            CreateViewModel(
                service);

        viewModel.FromDate =
            new DateTime(
                2026,
                8,
                22);

        viewModel.ToDate =
            new DateTime(
                2026,
                8,
                21);

        await viewModel.LoadAsync();

        Assert.Equal(
            0,
            service.GetEmployeesCallCount);

        Assert.Equal(
            0,
            service.GetCallCount);

        Assert.NotNull(
            viewModel.ErrorMessage);
    }

    [Fact]
    public async Task QueryFailure_SetsErrorAndClearsCollections()
    {
        var service =
            new StubWorkspaceQueryService
            {
                Snapshot =
                    CreateSnapshot(),

                GetException =
                    new InvalidOperationException(
                        "Test failure.")
            };

        var viewModel =
            CreateViewModel(
                service);

        await viewModel.LoadAsync();

        Assert.Empty(
            viewModel.AttendanceItems);

        Assert.Empty(
            viewModel.LeaveRequestItems);

        Assert.Equal(
            "Không thể tải dữ liệu chấm công và nghỉ phép.",
            viewModel.ErrorMessage);

        Assert.False(
            viewModel.IsLoading);
    }

    [Fact]
    public async Task ClearFilters_ResetsCurrentMonthAndEmployee()
    {
        Guid employeeId =
            Guid.NewGuid();

        var service =
            new StubWorkspaceQueryService
            {
                Employees =
                [
                    new AttendanceLeaveEmployeeItem(
                        employeeId,
                        "EMP-A",
                        "Nhân viên A")
                ]
            };

        var viewModel =
            CreateViewModel(
                service);

        await viewModel.LoadAsync();

        viewModel.SelectedEmployeeOption =
            viewModel.EmployeeOptions
                .Single(
                    option =>
                        option.EmployeeId ==
                        employeeId);

        viewModel.FromDate =
            new DateTime(
                2026,
                7,
                10);

        viewModel.ToDate =
            new DateTime(
                2026,
                7,
                15);

        await viewModel.ClearFiltersAsync();

        Assert.Equal(
            new DateTime(
                2026,
                8,
                1),
            viewModel.FromDate);

        Assert.Equal(
            new DateTime(
                2026,
                8,
                31),
            viewModel.ToDate);

        Assert.NotNull(
            viewModel.SelectedEmployeeOption);

        Assert.Null(
            viewModel
                .SelectedEmployeeOption!
                .EmployeeId);

        Assert.NotNull(
            service.LastQuery);

        Assert.Null(
            service.LastQuery!.EmployeeId);
    }

    private static AttendanceLeaveWorkspaceViewModel
        CreateViewModel(
            StubWorkspaceQueryService service)
    {
        return new AttendanceLeaveWorkspaceViewModel(
            service,
            new StubAttendancePunchService(),
            new StubAttendanceRecalculationService(),
            new StubLeaveRequestSubmissionService(),
            new TestLeaveRequestStatusService(),
            new NoOpDailyAttendanceGenerationService(),
            new FixedTimeProvider(
                new DateTimeOffset(
                    2026,
                    8,
                    21,
                    7,
                    0,
                    0,
                    TimeSpan.Zero)));
    }

    private static AttendanceLeaveWorkspaceSnapshot
        CreateSnapshot()
    {
        Guid employeeId =
            Guid.NewGuid();

        return new AttendanceLeaveWorkspaceSnapshot(
            [
                new AttendanceWorkspaceItem(
                    Guid.NewGuid(),
                    employeeId,
                    "EMP-A",
                    "Nhân viên A",
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
                    0)
            ],
            [
                new LeaveWorkspaceItem(
                    Guid.NewGuid(),
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
                        22),
                    new DateOnly(
                        2026,
                        8,
                        22),
                    LeaveRequestStatus.Pending,
                    new DateTime(
                        2026,
                        8,
                        20,
                        4,
                        0,
                        0,
                        DateTimeKind.Utc),
                    null)
            ]);
    }

    private sealed class StubWorkspaceQueryService
        : IAttendanceLeaveWorkspaceQueryService
    {
        public IReadOnlyList<LeaveTypeWorkspaceOption>
        LeaveTypes
        {
            get;
            set;
        } = [];

        public Task<IReadOnlyList<LeaveTypeWorkspaceOption>>
            GetActiveLeaveTypesAsync(
                CancellationToken cancellationToken = default)
        {
            return Task.FromResult(
                LeaveTypes);
        }

        public IReadOnlyList<AttendanceLeaveEmployeeItem>
            Employees
        {
            get;
            set;
        } = [];

        public AttendanceLeaveWorkspaceSnapshot Snapshot
        {
            get;
            set;
        } =
            new(
                [],
                []);

        public Exception? GetException
        {
            get;
            set;
        }

        public int GetEmployeesCallCount
        {
            get;
            private set;
        }

        public int GetCallCount
        {
            get;
            private set;
        }

        public AttendanceLeaveWorkspaceQuery? LastQuery
        {
            get;
            private set;
        }

        public Task<IReadOnlyList<AttendanceLeaveEmployeeItem>>
            GetEmployeesAsync(
                CancellationToken cancellationToken = default)
        {
            GetEmployeesCallCount++;

            return Task.FromResult(
                Employees);
        }

        public Task<AttendanceLeaveWorkspaceSnapshot>
            GetAsync(
                AttendanceLeaveWorkspaceQuery query,
                CancellationToken cancellationToken = default)
        {
            GetCallCount++;

            LastQuery =
                query;

            if (GetException is not null)
            {
                return Task.FromException<
                    AttendanceLeaveWorkspaceSnapshot>(
                        GetException);
            }

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

    private sealed class StubLeaveRequestSubmissionService
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
