using HrManagement.Application.Attendance.Schedules;
using HrManagement.Application.Workspaces.WorkSchedules;
using HrManagement.Desktop.Services;
using HrManagement.Desktop.ViewModels;

namespace HrManagement.Tests.Attendance;

public sealed class WorkScheduleWorkspaceDayEditorTests
{
    [Fact]
    public async Task LoadAsync_SelectsFirstDayAndLoadsEditor()
    {
        TestContext context =
            CreateContext();

        await context.ViewModel.LoadAsync();

        Assert.NotNull(
            context.ViewModel.SelectedScheduleDayItem);

        Assert.Equal(
            DayOfWeek.Monday,
            context.ViewModel.SelectedScheduleDayItem.DayOfWeek);

        Assert.True(
            context.ViewModel.DayEditorIsWorkingDay);

        Assert.Equal(
            "08:00",
            context.ViewModel.DayEditorStartTimeText);

        Assert.Equal(
            "17:00",
            context.ViewModel.DayEditorEndTimeText);

        Assert.Equal(
            "60",
            context.ViewModel.DayEditorBreakMinutesText);
    }

    [Fact]
    public async Task SelectingNonWorkingDay_ClearsEditorTimes()
    {
        TestContext context =
            CreateContext();

        await context.ViewModel.LoadAsync();

        context.ViewModel.SelectedScheduleDayItem =
            context.ViewModel
                .SelectedScheduleDays
                .Single(
                    day =>
                        day.DayOfWeek ==
                        DayOfWeek.Sunday);

        Assert.False(
            context.ViewModel.DayEditorIsWorkingDay);

        Assert.Null(
            context.ViewModel.DayEditorStartTimeText);

        Assert.Null(
            context.ViewModel.DayEditorEndTimeText);

        Assert.Equal(
            "0",
            context.ViewModel.DayEditorBreakMinutesText);
    }

    [Fact]
    public async Task TurningWorkingDayOn_AppliesUsefulDefaults()
    {
        TestContext context =
            CreateContext();

        await context.ViewModel.LoadAsync();

        context.ViewModel.SelectedScheduleDayItem =
            context.ViewModel
                .SelectedScheduleDays
                .Single(
                    day =>
                        day.DayOfWeek ==
                        DayOfWeek.Sunday);

        context.ViewModel.DayEditorIsWorkingDay =
            true;

        Assert.Equal(
            "08:00",
            context.ViewModel.DayEditorStartTimeText);

        Assert.Equal(
            "17:00",
            context.ViewModel.DayEditorEndTimeText);

        Assert.Equal(
            "60",
            context.ViewModel.DayEditorBreakMinutesText);
    }

    [Fact]
    public async Task SaveScheduleDayCommand_MapsWorkingDayRequest()
    {
        TestContext context =
            CreateContext();

        await context.ViewModel.LoadAsync();

        context.ViewModel.DayEditorStartTimeText =
            "22:00";

        context.ViewModel.DayEditorEndTimeText =
            "06:00";

        context.ViewModel.DayEditorBreakMinutesText =
            "30";

        await context.ViewModel
            .SaveScheduleDayCommand
            .ExecuteAsync(
                null);

        UpdateWorkScheduleDayRequest request =
            Assert.IsType<UpdateWorkScheduleDayRequest>(
                context.DayManagementService.LastRequest);

        Assert.Equal(
            context.ScheduleId,
            request.WorkScheduleId);

        Assert.Equal(
            DayOfWeek.Monday,
            request.DayOfWeek);

        Assert.True(
            request.IsWorkingDay);

        Assert.Equal(
            new TimeOnly(
                22,
                0),
            request.StartTime);

        Assert.Equal(
            new TimeOnly(
                6,
                0),
            request.EndTime);

        Assert.Equal(
            30,
            request.BreakMinutes);

        Assert.Contains(
            "Đã cập nhật Thứ 2",
            context.ViewModel.OperationMessage);
    }

    [Fact]
    public async Task SaveScheduleDayCommand_NonWorkingDayMapsEmptySchedule()
    {
        TestContext context =
            CreateContext();

        await context.ViewModel.LoadAsync();

        context.ViewModel.SelectedScheduleDayItem =
            context.ViewModel
                .SelectedScheduleDays
                .Single(
                    day =>
                        day.DayOfWeek ==
                        DayOfWeek.Sunday);

        await context.ViewModel
            .SaveScheduleDayCommand
            .ExecuteAsync(
                null);

        UpdateWorkScheduleDayRequest request =
            Assert.IsType<UpdateWorkScheduleDayRequest>(
                context.DayManagementService.LastRequest);

        Assert.False(
            request.IsWorkingDay);

        Assert.Null(
            request.StartTime);

        Assert.Null(
            request.EndTime);

        Assert.Equal(
            0,
            request.BreakMinutes);
    }

    [Fact]
    public async Task SaveScheduleDayCommand_InvalidTimeShowsValidationError()
    {
        TestContext context =
            CreateContext();

        await context.ViewModel.LoadAsync();

        context.ViewModel.DayEditorStartTimeText =
            "8 giờ";

        await context.ViewModel
            .SaveScheduleDayCommand
            .ExecuteAsync(
                null);

        Assert.Null(
            context.DayManagementService.LastRequest);

        Assert.Contains(
            "HH:mm",
            context.ViewModel.ErrorMessage);
    }

    [Fact]
    public async Task SaveScheduleDayCommand_ServiceFailureShowsBusinessError()
    {
        TestContext context =
            CreateContext();

        context.DayManagementService.Result =
            new WorkScheduleDayManagementResult(
                false,
                "Thời gian nghỉ phải ngắn hơn thời lượng ca làm việc.");

        await context.ViewModel.LoadAsync();

        await context.ViewModel
            .SaveScheduleDayCommand
            .ExecuteAsync(
                null);

        Assert.Equal(
            "Thời gian nghỉ phải ngắn hơn thời lượng ca làm việc.",
            context.ViewModel.ErrorMessage);

        Assert.Null(
            context.ViewModel.OperationMessage);
    }

    private static TestContext CreateContext()
    {
        Guid scheduleId =
            Guid.NewGuid();

        var queryService =
            new TestQueryService
            {
                Snapshot =
                    new WorkScheduleWorkspaceSnapshot(
                        [
                            new WorkScheduleWorkspaceScheduleItem(
                                scheduleId,
                                "OFFICE",
                                "Giờ hành chính",
                                "SE Asia Standard Time",
                                true)
                        ],
                        [
                            new WorkScheduleWorkspaceDayItem(
                                Guid.NewGuid(),
                                scheduleId,
                                DayOfWeek.Monday,
                                true,
                                new TimeOnly(
                                    8,
                                    0),
                                new TimeOnly(
                                    17,
                                    0),
                                60,
                                480),

                            new WorkScheduleWorkspaceDayItem(
                                Guid.NewGuid(),
                                scheduleId,
                                DayOfWeek.Sunday,
                                false,
                                null,
                                null,
                                0,
                                0)
                        ],
                        [])
            };

        var dayManagementService =
            new TestDayManagementService();

        var viewModel =
            new WorkScheduleWorkspaceViewModel(
                queryService,
                new TestAssignmentService(),
                new TestScheduleManagementService(),
                dayManagementService,
                new AlwaysConfirmService(),
                TimeProvider.System);

        return new TestContext(
            viewModel,
            dayManagementService,
            scheduleId);
    }

    private sealed record TestContext(
        WorkScheduleWorkspaceViewModel ViewModel,
        TestDayManagementService DayManagementService,
        Guid ScheduleId);

    private sealed class TestQueryService
        : IWorkScheduleWorkspaceQueryService
    {
        public WorkScheduleWorkspaceSnapshot Snapshot
        {
            get;
            init;
        } =
            new(
                [],
                [],
                []);

        public Task<IReadOnlyList<WorkScheduleWorkspaceEmployeeItem>>
            GetEmployeesAsync(
                CancellationToken cancellationToken = default)
        {
            return Task.FromResult<
                IReadOnlyList<WorkScheduleWorkspaceEmployeeItem>>(
                    []);
        }

        public Task<WorkScheduleWorkspaceSnapshot> GetAsync(
            WorkScheduleWorkspaceQuery query,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(
                Snapshot);
        }
    }

    private sealed class TestAssignmentService
        : IEmployeeWorkScheduleAssignmentService
    {
        public Task<AssignEmployeeWorkScheduleResult> AssignAsync(
            AssignEmployeeWorkScheduleRequest request,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(
                new AssignEmployeeWorkScheduleResult(
                    true));
        }
    }

    private sealed class TestScheduleManagementService
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

    private sealed class TestDayManagementService
        : IWorkScheduleDayManagementService
    {
        public UpdateWorkScheduleDayRequest?
            LastRequest
        {
            get;
            private set;
        }

        public WorkScheduleDayManagementResult Result
        {
            get;
            set;
        } =
            new(
                true);

        public Task<WorkScheduleDayManagementResult> UpdateAsync(
            UpdateWorkScheduleDayRequest request,
            CancellationToken cancellationToken = default)
        {
            LastRequest =
                request;

            return Task.FromResult(
                Result);
        }
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
