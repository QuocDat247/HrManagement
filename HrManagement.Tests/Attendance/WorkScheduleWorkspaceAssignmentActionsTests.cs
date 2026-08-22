using HrManagement.Application.Attendance.Schedules;
using HrManagement.Application.Workspaces.WorkSchedules;
using HrManagement.Desktop.Services;
using HrManagement.Desktop.ViewModels;

namespace HrManagement.Tests.Attendance;

public sealed class WorkScheduleWorkspaceAssignmentActionsTests
{
    private static readonly DateTimeOffset FixedNow =
        new(
            2026,
            8,
            21,
            20,
            0,
            0,
            TimeSpan.FromHours(
                7));

    [Fact]
    public async Task AssignScheduleCommand_RequiresConcreteEmployee()
    {
        TestContext context =
            CreateContext();

        await context.ViewModel.LoadAsync();

        Assert.False(
            context.ViewModel
                .AssignScheduleCommand
                .CanExecute(
                    null));

        context.ViewModel.SelectedEmployeeOption =
            context.ViewModel
                .EmployeeOptions
                .Single(
                    option =>
                        option.EmployeeId ==
                        context.EmployeeId);

        Assert.True(
            context.ViewModel
                .AssignScheduleCommand
                .CanExecute(
                    null));
    }

    [Fact]
    public async Task AssignScheduleAsync_MapsRequestAndRefreshes()
    {
        TestContext context =
            CreateContext();

        await context.ViewModel.LoadAsync();

        context.ViewModel.SelectedEmployeeOption =
            context.ViewModel
                .EmployeeOptions
                .Single(
                    option =>
                        option.EmployeeId ==
                        context.EmployeeId);

        context.ViewModel.AssignmentEffectiveFrom =
            new DateTime(
                2026,
                8,
                25);

        int queryCallsBefore =
            context.QueryService.GetAsyncCallCount;

        await context.ViewModel
            .AssignScheduleCommand
            .ExecuteAsync(
                null);

        AssignEmployeeWorkScheduleRequest request =
            Assert.IsType<AssignEmployeeWorkScheduleRequest>(
                context.AssignmentService.LastRequest);

        Assert.Equal(
            context.EmployeeId,
            request.EmployeeId);

        Assert.Equal(
            context.ScheduleId,
            request.WorkScheduleId);

        Assert.Equal(
            new DateOnly(
                2026,
                8,
                25),
            request.EffectiveFrom);

        Assert.True(
            context.QueryService.GetAsyncCallCount >
            queryCallsBefore);

        Assert.Contains(
            "Đã phân OFFICE - Giờ hành chính",
            context.ViewModel.OperationMessage);

        Assert.Null(
            context.ViewModel.ErrorMessage);
    }

    [Fact]
    public async Task AssignScheduleAsync_ServiceFailureShowsBusinessError()
    {
        TestContext context =
            CreateContext();

        context.AssignmentService.Result =
            new AssignEmployeeWorkScheduleResult(
                false,
                "Lịch làm việc mới phải khác lịch đang được phân.");

        await context.ViewModel.LoadAsync();

        context.ViewModel.SelectedEmployeeOption =
            context.ViewModel
                .EmployeeOptions
                .Single(
                    option =>
                        option.EmployeeId ==
                        context.EmployeeId);

        await context.ViewModel
            .AssignScheduleCommand
            .ExecuteAsync(
                null);

        Assert.Equal(
            "Lịch làm việc mới phải khác lịch đang được phân.",
            context.ViewModel.ErrorMessage);

        Assert.Null(
            context.ViewModel.OperationMessage);
    }

    [Fact]
    public async Task LoadAsync_UsesOnlyActiveSchedulesForAssignment()
    {
        TestContext context =
            CreateContext(
                includeInactiveSchedule: true);

        await context.ViewModel.LoadAsync();

        WorkScheduleWorkspaceScheduleItem schedule =
            Assert.Single(
                context.ViewModel.ActiveScheduleItems);

        Assert.True(
            schedule.IsActive);

        Assert.Equal(
            "OFFICE",
            schedule.Code);

        Assert.Equal(
            2,
            context.ViewModel.ScheduleItems.Count);
    }

    private static TestContext CreateContext(
        bool includeInactiveSchedule = false)
    {
        Guid employeeId =
            Guid.NewGuid();

        Guid scheduleId =
            Guid.NewGuid();

        var schedules =
            new List<WorkScheduleWorkspaceScheduleItem>
            {
                new(
                    scheduleId,
                    "OFFICE",
                    "Giờ hành chính",
                    "SE Asia Standard Time",
                    true)
            };

        if (includeInactiveSchedule)
        {
            schedules.Add(
                new WorkScheduleWorkspaceScheduleItem(
                    Guid.NewGuid(),
                    "OLD",
                    "Lịch cũ",
                    "SE Asia Standard Time",
                    false));
        }

        var queryService =
            new TestWorkspaceQueryService
            {
                Employees =
                [
                    new WorkScheduleWorkspaceEmployeeItem(
                        employeeId,
                        "EMP006",
                        "Nguyễn Chí Thành")
                ],

                Snapshot =
                    new WorkScheduleWorkspaceSnapshot(
                        schedules,
                        [],
                        [])
            };

        var assignmentService =
            new TestAssignmentService();

        var viewModel =
            new WorkScheduleWorkspaceViewModel(
                queryService,
                assignmentService,
                new TestWorkScheduleManagementService(),
                new TestWorkScheduleDayManagementService(),
                new AlwaysConfirmService(),
                new FixedTimeProvider(
                    FixedNow));

        return new TestContext(
            viewModel,
            queryService,
            assignmentService,
            employeeId,
            scheduleId);
    }

    private sealed record TestContext(
        WorkScheduleWorkspaceViewModel ViewModel,
        TestWorkspaceQueryService QueryService,
        TestAssignmentService AssignmentService,
        Guid EmployeeId,
        Guid ScheduleId);

    private sealed class TestWorkspaceQueryService
        : IWorkScheduleWorkspaceQueryService
    {
        public IReadOnlyList<WorkScheduleWorkspaceEmployeeItem>
            Employees
        {
            get;
            init;
        } = [];

        public WorkScheduleWorkspaceSnapshot Snapshot
        {
            get;
            init;
        } =
            new(
                [],
                [],
                []);

        public int GetAsyncCallCount
        {
            get;
            private set;
        }

        public Task<IReadOnlyList<WorkScheduleWorkspaceEmployeeItem>>
            GetEmployeesAsync(
                CancellationToken cancellationToken = default)
        {
            return Task.FromResult(
                Employees);
        }

        public Task<WorkScheduleWorkspaceSnapshot> GetAsync(
            WorkScheduleWorkspaceQuery query,
            CancellationToken cancellationToken = default)
        {
            GetAsyncCallCount++;

            return Task.FromResult(
                Snapshot);
        }
    }

    private sealed class TestAssignmentService
        : IEmployeeWorkScheduleAssignmentService
    {
        public AssignEmployeeWorkScheduleRequest?
            LastRequest
        {
            get;
            private set;
        }

        public AssignEmployeeWorkScheduleResult Result
        {
            get;
            set;
        } =
            new(
                true);

        public Task<AssignEmployeeWorkScheduleResult> AssignAsync(
            AssignEmployeeWorkScheduleRequest request,
            CancellationToken cancellationToken = default)
        {
            LastRequest =
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
            DateTimeOffset localNow)
        {
            _utcNow =
                localNow.ToUniversalTime();
        }

        public override DateTimeOffset GetUtcNow()
        {
            return _utcNow;
        }

        public override TimeZoneInfo LocalTimeZone =>
            TimeZoneInfo.CreateCustomTimeZone(
                "Test-SE-Asia",
                TimeSpan.FromHours(
                    7),
                "Test-SE-Asia",
                "Test-SE-Asia");
    }

    private sealed class TestWorkScheduleManagementService
    : IWorkScheduleManagementService
    {
        public Task<WorkScheduleManagementResult> DeleteAsync(
            Guid workScheduleId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(
                new WorkScheduleManagementResult(
                    true,
                    workScheduleId));
        }

        public Task<WorkScheduleManagementResult> CloneAsync(
            CloneWorkScheduleRequest request,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(
                new WorkScheduleManagementResult(
                    true,
                    Guid.NewGuid()));
        }

        public Task<WorkScheduleManagementResult> CreateAsync(
            CreateWorkScheduleRequest request,
            CancellationToken cancellationToken = default)
            => Task.FromResult(
                new WorkScheduleManagementResult(
                    true,
                    Guid.NewGuid()));

        public Task<WorkScheduleManagementResult> UpdateAsync(
            UpdateWorkScheduleRequest request,
            CancellationToken cancellationToken = default)
            => Task.FromResult(
                new WorkScheduleManagementResult(
                    true,
                    request.WorkScheduleId));

        public Task<WorkScheduleManagementResult> DeactivateAsync(
            Guid workScheduleId,
            CancellationToken cancellationToken = default)
            => Task.FromResult(
                new WorkScheduleManagementResult(
                    true,
                    workScheduleId));

        public Task<WorkScheduleManagementResult> ReactivateAsync(
            Guid workScheduleId,
            CancellationToken cancellationToken = default)
            => Task.FromResult(
                new WorkScheduleManagementResult(
                    true,
                    workScheduleId));
    }

    private sealed class TestWorkScheduleDayManagementService
    : IWorkScheduleDayManagementService
    {
        public Task<WorkScheduleDayManagementResult> UpdateAsync(
            UpdateWorkScheduleDayRequest request,
            CancellationToken cancellationToken = default)
            => Task.FromResult(
                new WorkScheduleDayManagementResult(
                    true));
    }

    private sealed class AlwaysConfirmService
    : IUserConfirmationService
    {
        public bool Confirm(
            string title,
            string message)
        {
            return true;
        }
    }
}
