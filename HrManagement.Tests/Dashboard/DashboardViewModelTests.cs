using HrManagement.Application.Dashboard;
using HrManagement.Application.Dashboard.Analytics;
using HrManagement.Desktop.Services;
using HrManagement.Desktop.ViewModels;
using HrManagement.Domain.Employees;

namespace HrManagement.Tests.Dashboard;

public sealed class DashboardViewModelTests
{
    [Fact]
    public async Task LoadAsync_WhenServiceSucceeds_PopulatesSummary()
    {
        var service = new StubDashboardService(
            new DashboardSummary(
            TotalEmployees: 128,
            ActiveEmployees: 119,
            EmployeesOnLeave: 6,
            InactiveEmployees: 3,
            EmployeesMissingProfileInformation: 0,
            RecentEmployees: Array.Empty<RecentEmployee>(),
            Departments: Array.Empty<DepartmentEmployeeSummary>()));

        var viewModel = new DashboardViewModel(
                        service,
                        new StubEmployeeNavigationService(),
                        new StubWorkforceAnalyticsService());

        await viewModel.LoadAsync();

        Assert.Equal(128, viewModel.TotalEmployees);
        Assert.Equal(119, viewModel.ActiveEmployees);
        Assert.Equal(6, viewModel.EmployeesOnLeave);
        Assert.Equal(3, viewModel.InactiveEmployees);


        Assert.False(viewModel.IsLoading);
        Assert.Null(viewModel.ErrorMessage);
    }

    [Fact]
    public async Task LoadAsync_WhenServiceFails_SetsErrorMessage()
    {
        var service = new FailingDashboardService();

        var viewModel = new DashboardViewModel(
                        service,
                        new StubEmployeeNavigationService(),
                        new StubWorkforceAnalyticsService());

        await viewModel.LoadAsync();

        Assert.False(viewModel.IsLoading);
        Assert.Equal(
            "Không thể tải dữ liệu Dashboard.",
            viewModel.ErrorMessage);
    }

    private sealed class StubDashboardService : IDashboardService
    {
        private readonly DashboardSummary _summary;

        public StubDashboardService(DashboardSummary summary)
        {
            _summary = summary;
        }

        public Task<DashboardSummary> GetSummaryAsync(
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_summary);
        }
    }

    private sealed class FailingDashboardService : IDashboardService
    {
        public Task<DashboardSummary> GetSummaryAsync(
            CancellationToken cancellationToken = default)
        {
            return Task.FromException<DashboardSummary>(
                new InvalidOperationException("Test failure"));
        }
    }

    [Fact]
    public async Task LoadAsync_WhenServiceReturnsRecentEmployees_PopulatesRecentEmployees()
    {
        var firstEmployee = new RecentEmployee(
            Guid.NewGuid(),
            "EMP010",
            "Nguyễn Minh Anh",
            "Công nghệ thông tin",
            "Lập trình viên",
            new DateOnly(2026, 8, 10),
            EmployeeStatus.Active);

        var secondEmployee = new RecentEmployee(
            Guid.NewGuid(),
            "EMP011",
            "Trần Thu Hà",
            "Nhân sự",
            "Chuyên viên nhân sự",
            new DateOnly(2026, 8, 9),
            EmployeeStatus.OnLeave);

        IReadOnlyList<RecentEmployee> recentEmployees =
            [firstEmployee, secondEmployee];

        var service =
            new RecentEmployeesDashboardService(
                new DashboardSummary(
                    TotalEmployees: 2,
                    ActiveEmployees: 1,
                    EmployeesOnLeave: 1,
                    InactiveEmployees: 0,
                    EmployeesMissingProfileInformation: 0,
                    RecentEmployees: recentEmployees,
                    Departments: Array.Empty<DepartmentEmployeeSummary>()));

        var viewModel = new DashboardViewModel(
                        service,
                        new StubEmployeeNavigationService(),
                        new StubWorkforceAnalyticsService());

        await viewModel.LoadAsync();

        Assert.Equal(2, viewModel.RecentEmployees.Count);

        Assert.Collection(
            viewModel.RecentEmployees,
            employee =>
            {
                Assert.Equal("EMP010", employee.EmployeeCode);
                Assert.Equal(
                    new DateOnly(2026, 8, 10),
                    employee.HireDate);
            },
            employee =>
            {
                Assert.Equal("EMP011", employee.EmployeeCode);
                Assert.Equal(
                    EmployeeStatus.OnLeave,
                    employee.Status);
            });
    }

    private sealed class RecentEmployeesDashboardService
    : IDashboardService
    {
        private readonly DashboardSummary _summary;

        public RecentEmployeesDashboardService(
            DashboardSummary summary)
        {
            _summary = summary;
        }

        public Task<DashboardSummary> GetSummaryAsync(
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_summary);
        }
    }

    [Fact]
    public async Task LoadAsync_WhenServiceReturnsDepartments_PopulatesDepartments()
    {
        var accounting =
            new DepartmentEmployeeSummary(
                Department: "Kế toán",
                TotalEmployees: 3,
                ActiveEmployees: 2,
                EmployeesOnLeave: 0,
                InactiveEmployees: 1);

        var humanResources =
            new DepartmentEmployeeSummary(
                Department: "Nhân sự",
                TotalEmployees: 2,
                ActiveEmployees: 1,
                EmployeesOnLeave: 1,
                InactiveEmployees: 0);

        IReadOnlyList<DepartmentEmployeeSummary> departments =
            [accounting, humanResources];

        var service =
            new RecentEmployeesDashboardService(
                new DashboardSummary(
                    TotalEmployees: 5,
                    ActiveEmployees: 3,
                    EmployeesOnLeave: 1,
                    InactiveEmployees: 1,
                    EmployeesMissingProfileInformation: 0,
                    RecentEmployees: Array.Empty<RecentEmployee>(),
                    Departments: departments));

        var viewModel = new DashboardViewModel(
                        service,
                        new StubEmployeeNavigationService(),
                        new StubWorkforceAnalyticsService());

        await viewModel.LoadAsync();

        Assert.Equal(2, viewModel.Departments.Count);

        Assert.Collection(
            viewModel.Departments,
            department =>
            {
                Assert.Equal(
                    "Kế toán",
                    department.Department);

                Assert.Equal(
                    3,
                    department.TotalEmployees);

                Assert.Equal(
                    2,
                    department.ActiveEmployees);

                Assert.Equal(
                    1,
                    department.InactiveEmployees);
            },
            department =>
            {
                Assert.Equal(
                    "Nhân sự",
                    department.Department);

                Assert.Equal(
                    2,
                    department.TotalEmployees);

                Assert.Equal(
                    1,
                    department.EmployeesOnLeave);
            });
    }

    [Fact]
    public async Task LoadAsync_WhenServiceReturnsMissingProfileCount_PopulatesMissingProfileCount()
    {
        var service =
            new RecentEmployeesDashboardService(
                new DashboardSummary(
                    TotalEmployees: 6,
                    ActiveEmployees: 4,
                    EmployeesOnLeave: 1,
                    InactiveEmployees: 1,
                    EmployeesMissingProfileInformation: 3,
                    RecentEmployees: Array.Empty<RecentEmployee>(),
                    Departments: Array.Empty<DepartmentEmployeeSummary>()));

        var viewModel = new DashboardViewModel(
                        service,
                        new StubEmployeeNavigationService(),
                        new StubWorkforceAnalyticsService());

        await viewModel.LoadAsync();

        Assert.Equal(
            3,
            viewModel.EmployeesMissingProfileInformation);
    }

    private sealed class StubEmployeeNavigationService
    : IEmployeeNavigationService
    {
        public int ShowProfileCompletionRequiredCallCount
        {
            get;
            private set;
        }

        public Task ShowEmployeesRequiringProfileCompletionAsync()
        {
            ShowProfileCompletionRequiredCallCount++;

            return Task.CompletedTask;
        }
    }

    [Fact]
    public async Task ShowProfileCompletionRequiredCommand_WhenProfilesAreMissing_NavigatesToEmployees()
    {
        var dashboardService =
            new RecentEmployeesDashboardService(
                new DashboardSummary(
                    TotalEmployees: 3,
                    ActiveEmployees: 3,
                    EmployeesOnLeave: 0,
                    InactiveEmployees: 0,
                    EmployeesMissingProfileInformation: 2,
                    RecentEmployees: Array.Empty<RecentEmployee>(),
                    Departments:
                        Array.Empty<DepartmentEmployeeSummary>()));

        var navigationService =
            new StubEmployeeNavigationService();

        var viewModel = new DashboardViewModel(
                        dashboardService,
                        navigationService,
                        new StubWorkforceAnalyticsService());

        await viewModel.LoadAsync();

        Assert.True(
            viewModel
                .ShowProfileCompletionRequiredCommand
                .CanExecute(null));

        await viewModel
            .ShowProfileCompletionRequiredCommand
            .ExecuteAsync(null);

        Assert.Equal(
            1,
            navigationService
                .ShowProfileCompletionRequiredCallCount);
    }

    private sealed class StubWorkforceAnalyticsService
    : IWorkforceAnalyticsService
    {
        public int? LastYear { get; private set; }

        public WorkforceAnalyticsGrouping?
            LastGrouping
        { get; private set; }

        public Func<
            int,
            WorkforceAnalyticsGrouping,
            WorkforceMovementSummary>?
            ResultFactory
        { get; set; }

        public Task<WorkforceMovementSummary>
            GetWorkforceMovementAsync(
                int year,
                WorkforceAnalyticsGrouping grouping =
                    WorkforceAnalyticsGrouping.Monthly,
                CancellationToken cancellationToken = default)
        {
            LastYear = year;
            LastGrouping = grouping;

            WorkforceMovementSummary result =
                ResultFactory?.Invoke(
                    year,
                    grouping)
                ?? new WorkforceMovementSummary(
                    Year: year,
                    Grouping: grouping,
                    BeginningHeadcount: 0,
                    EndingHeadcount: 0,
                    TotalNewHires: 0,
                    TotalSeparations: 0,
                    NetChange: 0,
                    AverageHeadcount: 0,
                    TurnoverRate: 0,
                    EmployeesWithUnknownTerminationDate: 0,
                    Periods:
                        Array.Empty<WorkforceMovementPeriod>());

            return Task.FromResult(result);
        }
    }

    [Fact]
    public async Task LoadAnalyticsAsync_WithMonthlySummary_PopulatesAnalyticsProperties()
    {
        var analyticsService =
            new StubWorkforceAnalyticsService
            {
                ResultFactory = (year, grouping) =>
                    new WorkforceMovementSummary(
                        Year: year,
                        Grouping: grouping,
                        BeginningHeadcount: 10,
                        EndingHeadcount: 12,
                        TotalNewHires: 4,
                        TotalSeparations: 2,
                        NetChange: 2,
                        AverageHeadcount: 11m,
                        TurnoverRate: 18.18m,
                        EmployeesWithUnknownTerminationDate: 1,
                        Periods:
                        [
                            new WorkforceMovementPeriod(
                            PeriodNumber: 1,
                            StartDate:
                                new DateOnly(2026, 1, 1),
                            EndDate:
                                new DateOnly(2026, 1, 31),
                            NewHires: 3,
                            Separations: 1,
                            BeginningHeadcount: 10,
                            EndingHeadcount: 12,
                            AverageHeadcount: 11m,
                            TurnoverRate: 9.09m,
                            NetChange: 2),

                        new WorkforceMovementPeriod(
                            PeriodNumber: 2,
                            StartDate:
                                new DateOnly(2026, 2, 1),
                            EndDate:
                                new DateOnly(2026, 2, 28),
                            NewHires: 1,
                            Separations: 1,
                            BeginningHeadcount: 12,
                            EndingHeadcount: 12,
                            AverageHeadcount: 12m,
                            TurnoverRate: 8.33m,
                            NetChange: 0)
                        ])
            };

        var viewModel =
            new DashboardViewModel(
                new RecentEmployeesDashboardService(
                    new DashboardSummary(
                        0,
                        0,
                        0,
                        0,
                        0,
                        Array.Empty<RecentEmployee>(),
                        Array.Empty<DepartmentEmployeeSummary>())),
                new StubEmployeeNavigationService(),
                analyticsService);

        viewModel.SelectedAnalyticsYear = 2026;

        await viewModel.LoadAnalyticsAsync();

        Assert.Equal(10, viewModel.AnalyticsBeginningHeadcount);
        Assert.Equal(12, viewModel.AnalyticsEndingHeadcount);
        Assert.Equal(4, viewModel.TotalNewHires);
        Assert.Equal(2, viewModel.TotalSeparations);
        Assert.Equal(2, viewModel.WorkforceNetChange);
        Assert.Equal(11m, viewModel.WorkforceAverageHeadcount);
        Assert.Equal(18.18m, viewModel.WorkforceTurnoverRate);

        Assert.Equal(
            1,
            viewModel.EmployeesWithUnknownTerminationDate);

        Assert.Equal(
            2,
            viewModel.WorkforceMovementItems.Count);

        Assert.Equal(
            "T1",
            viewModel.WorkforceMovementItems[0].Label);

        Assert.Equal(
            "Tháng 1",
            viewModel.WorkforceMovementItems[0].DisplayName);

        Assert.Equal(
            3,
            viewModel.WorkforceMovementChartMaximum);

        Assert.Equal(
            2,
            viewModel.WorkforceMovementChartMidpoint);

        Assert.Equal(
            "+2",
            viewModel.WorkforceNetChangeDisplay);
    }

    [Fact]
    public async Task RefreshAnalyticsCommand_WithQuarterlySelection_UsesSelectedYearAndGrouping()
    {
        var analyticsService =
            new StubWorkforceAnalyticsService();

        var viewModel =
            new DashboardViewModel(
                new RecentEmployeesDashboardService(
                    new DashboardSummary(
                        0,
                        0,
                        0,
                        0,
                        0,
                        Array.Empty<RecentEmployee>(),
                        Array.Empty<DepartmentEmployeeSummary>())),
                new StubEmployeeNavigationService(),
                analyticsService);

        viewModel.SelectedAnalyticsYear = 2025;

        viewModel.SelectedAnalyticsGroupingOption =
            viewModel.AnalyticsGroupingOptions
                .Single(option =>
                    option.Grouping ==
                        WorkforceAnalyticsGrouping.Quarterly);

        await viewModel.RefreshAnalyticsCommand
            .ExecuteAsync(null);

        Assert.Equal(
            2025,
            analyticsService.LastYear);

        Assert.Equal(
            WorkforceAnalyticsGrouping.Quarterly,
            analyticsService.LastGrouping);
    }

    [Fact]
    public async Task LoadAnalyticsAsync_WhenThereIsNoMovement_UsesEmptyStateAndSafeChartScale()
    {
        var analyticsService =
            new StubWorkforceAnalyticsService
            {
                ResultFactory = (year, grouping) =>
                    new WorkforceMovementSummary(
                        Year: year,
                        Grouping: grouping,
                        BeginningHeadcount: 5,
                        EndingHeadcount: 5,
                        TotalNewHires: 0,
                        TotalSeparations: 0,
                        NetChange: 0,
                        AverageHeadcount: 5m,
                        TurnoverRate: 0m,
                        EmployeesWithUnknownTerminationDate: 0,
                        Periods:
                        Enumerable.Range(1, 12)
                            .Select(month =>
                                new WorkforceMovementPeriod(
                                    PeriodNumber: month,
                                    StartDate:
                                        new DateOnly(2026, month, 1),
                                    EndDate:
                                        new DateOnly(
                                            2026,
                                            month,
                                            DateTime.DaysInMonth(
                                                2026,
                                                month)),
                                    NewHires: 0,
                                    Separations: 0,
                                    BeginningHeadcount: 5,
                                    EndingHeadcount: 5,
                                    AverageHeadcount: 5m,
                                    TurnoverRate: 0m,
                                    NetChange: 0))
                            .ToList())
            };

        var viewModel =
            new DashboardViewModel(
                new RecentEmployeesDashboardService(
                    new DashboardSummary(
                        0,
                        0,
                        0,
                        0,
                        0,
                        Array.Empty<RecentEmployee>(),
                        Array.Empty<DepartmentEmployeeSummary>())),
                new StubEmployeeNavigationService(),
                analyticsService);

        await viewModel.LoadAnalyticsAsync();

        Assert.False(
            viewModel.HasWorkforceMovementData);

        Assert.Equal(
            1,
            viewModel.WorkforceMovementChartMaximum);

        Assert.Equal(
            "0",
            viewModel.WorkforceNetChangeDisplay);
    }
}
