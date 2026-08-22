using HrManagement.Application.Attendance.Schedules;
using HrManagement.Application.Workspaces.WorkSchedules;
using HrManagement.Desktop.Services;
using HrManagement.Desktop.ViewModels;

namespace HrManagement.Tests.Attendance;

public sealed class WorkScheduleWorkspaceViewModelTests
{
    [Fact]
    public async Task LoadAsync_LoadsEmployeesSchedulesDaysAndAssignments()
    {
        var service =
            CreateService();

        var viewModel =
            CreateViewModel(
                service);

        await viewModel.LoadAsync();

        Assert.Equal(
            3,
            viewModel.EmployeeOptions.Count);

        Assert.Equal(
            2,
            viewModel.ScheduleItems.Count);

        Assert.Single(
            viewModel.AssignmentItems);

        Assert.NotNull(
            viewModel.SelectedScheduleItem);

        Assert.Equal(
            "OFFICE",
            viewModel.SelectedScheduleItem.Code);

        Assert.Equal(
            2,
            viewModel.SelectedScheduleDays.Count);

        Assert.Null(
            viewModel.ErrorMessage);
    }

    [Fact]
    public async Task EmployeeSearch_FiltersByCode()
    {
        var service =
            CreateService();

        var viewModel =
            CreateViewModel(
                service);

        await viewModel.LoadAsync();

        viewModel.EmployeeSearchText =
            "EMP006";

        Assert.Equal(
            2,
            viewModel.FilteredEmployeeOptions.Count);

        Assert.Null(
            viewModel.FilteredEmployeeOptions[0].EmployeeId);

        Assert.Contains(
            "EMP006",
            viewModel.FilteredEmployeeOptions[1].DisplayName);
    }

    [Fact]
    public async Task EmployeeSearch_FiltersByNameIgnoringCase()
    {
        var service =
            CreateService();

        var viewModel =
            CreateViewModel(
                service);

        await viewModel.LoadAsync();

        viewModel.EmployeeSearchText =
            "chí thành";

        WorkScheduleEmployeeFilterOption employee =
            Assert.Single(
                viewModel.FilteredEmployeeOptions.Where(
                    item =>
                        item.EmployeeId.HasValue));

        Assert.Contains(
            "Nguyễn Chí Thành",
            employee.DisplayName);
    }

    [Fact]
    public async Task LoadAsync_SelectedEmployeeIsMappedToQuery()
    {
        var service =
            CreateService();

        var viewModel =
            CreateViewModel(
                service);

        await viewModel.LoadAsync();

        WorkScheduleEmployeeFilterOption employee =
            viewModel.EmployeeOptions.Single(
                item =>
                    item.EmployeeId ==
                    service.FirstEmployeeId);

        viewModel.SelectedEmployeeOption =
            employee;

        await viewModel.LoadAsync();

        Assert.Equal(
            service.FirstEmployeeId,
            service.LastQuery?
                .EmployeeId);
    }

    [Fact]
    public async Task SelectingSchedule_UpdatesVisibleDays()
    {
        var service =
            CreateService();

        var viewModel =
            CreateViewModel(
                service);

        await viewModel.LoadAsync();

        WorkScheduleWorkspaceScheduleItem second =
            viewModel.ScheduleItems.Single(
                schedule =>
                    schedule.Code ==
                    "NIGHT");

        viewModel.SelectedScheduleItem =
            second;

        WorkScheduleWorkspaceDayItem day =
            Assert.Single(
                viewModel.SelectedScheduleDays);

        Assert.Equal(
            second.WorkScheduleId,
            day.WorkScheduleId);

        Assert.Equal(
            DayOfWeek.Saturday,
            day.DayOfWeek);
    }

    [Fact]
    public async Task ClearFilters_ResetsEmployeeFilterAndReloads()
    {
        var service =
            CreateService();

        var viewModel =
            CreateViewModel(
                service);

        await viewModel.LoadAsync();

        viewModel.EmployeeSearchText =
            "EMP006";

        viewModel.SelectedEmployeeOption =
            viewModel.EmployeeOptions.Single(
                item =>
                    item.EmployeeId ==
                    service.FirstEmployeeId);

        await viewModel.ClearFiltersCommand.ExecuteAsync(
            null);

        Assert.Null(
            viewModel.EmployeeSearchText);

        Assert.Null(
            viewModel.SelectedEmployeeOption?
                .EmployeeId);

        Assert.Null(
            service.LastQuery?
                .EmployeeId);
    }

    private static WorkScheduleWorkspaceViewModel
    CreateViewModel(
        StubWorkScheduleWorkspaceQueryService service)
    {
        return new WorkScheduleWorkspaceViewModel(
            service,
            new NoOpAssignmentService(),
            new NoOpWorkScheduleManagementService(),
            new NoOpWorkScheduleDayManagementService(),
            new AlwaysConfirmService(),
            TimeProvider.System);
    }

    private static StubWorkScheduleWorkspaceQueryService
        CreateService()
    {
        Guid firstEmployeeId =
            Guid.NewGuid();

        Guid secondEmployeeId =
            Guid.NewGuid();

        Guid officeId =
            Guid.NewGuid();

        Guid nightId =
            Guid.NewGuid();

        var service =
            new StubWorkScheduleWorkspaceQueryService
            {
                FirstEmployeeId =
                    firstEmployeeId,

                Employees =
                [
                    new WorkScheduleWorkspaceEmployeeItem(
                        firstEmployeeId,
                        "EMP006",
                        "Nguyễn Chí Thành"),

                    new WorkScheduleWorkspaceEmployeeItem(
                        secondEmployeeId,
                        "EMP007",
                        "Trần Minh Anh")
                ],

                Snapshot =
                    new WorkScheduleWorkspaceSnapshot(
                        [
                            new WorkScheduleWorkspaceScheduleItem(
                                officeId,
                                "OFFICE",
                                "Giờ hành chính",
                                "SE Asia Standard Time",
                                true),

                            new WorkScheduleWorkspaceScheduleItem(
                                nightId,
                                "NIGHT",
                                "Ca đêm",
                                "SE Asia Standard Time",
                                true)
                        ],
                        [
                            new WorkScheduleWorkspaceDayItem(
                                Guid.NewGuid(),
                                officeId,
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
                                officeId,
                                DayOfWeek.Tuesday,
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
                                nightId,
                                DayOfWeek.Saturday,
                                true,
                                new TimeOnly(
                                    22,
                                    0),
                                new TimeOnly(
                                    6,
                                    0),
                                30,
                                450)
                        ],
                        [
                            new WorkScheduleWorkspaceAssignmentItem(
                                Guid.NewGuid(),
                                firstEmployeeId,
                                "EMP006",
                                "Nguyễn Chí Thành",
                                Guid.NewGuid(),
                                officeId,
                                "OFFICE",
                                "Giờ hành chính",
                                new DateOnly(
                                    2026,
                                    8,
                                    1),
                                null,
                                true)
                        ])
            };

        return service;
    }

    private sealed class StubWorkScheduleWorkspaceQueryService
        : IWorkScheduleWorkspaceQueryService
    {
        public Guid FirstEmployeeId
        {
            get;
            set;
        }

        public IReadOnlyList<WorkScheduleWorkspaceEmployeeItem>
            Employees
        {
            get;
            set;
        } = [];

        public WorkScheduleWorkspaceSnapshot Snapshot
        {
            get;
            set;
        } =
            new(
                [],
                [],
                []);

        public WorkScheduleWorkspaceQuery?
            LastQuery
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
            LastQuery =
                query;

            return Task.FromResult(
                Snapshot);
        }
    }

    private sealed class NoOpAssignmentService
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

    private sealed class NoOpWorkScheduleManagementService
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
        {
            return Task.FromResult(
                new WorkScheduleManagementResult(
                    true,
                    Guid.NewGuid()));
        }

        public Task<WorkScheduleManagementResult> UpdateAsync(
            UpdateWorkScheduleRequest request,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(
                new WorkScheduleManagementResult(
                    true,
                    request.WorkScheduleId));
        }

        public Task<WorkScheduleManagementResult> DeactivateAsync(
            Guid workScheduleId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(
                new WorkScheduleManagementResult(
                    true,
                    workScheduleId));
        }

        public Task<WorkScheduleManagementResult> ReactivateAsync(
            Guid workScheduleId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(
                new WorkScheduleManagementResult(
                    true,
                    workScheduleId));
        }
    }

    private sealed class NoOpWorkScheduleDayManagementService
    : IWorkScheduleDayManagementService
    {
        public Task<WorkScheduleDayManagementResult> UpdateAsync(
            UpdateWorkScheduleDayRequest request,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(
                new WorkScheduleDayManagementResult(
                    true));
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
