using HrManagement.Application.Dashboard;
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
            RecentEmployees: Array.Empty<RecentEmployee>(),
            Departments: Array.Empty<DepartmentEmployeeSummary>()));

        var viewModel = new DashboardViewModel(service);

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

        var viewModel = new DashboardViewModel(service);

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
                    RecentEmployees: recentEmployees,
                    Departments: Array.Empty<DepartmentEmployeeSummary>()));

        var viewModel =
            new DashboardViewModel(service);

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
                    RecentEmployees: Array.Empty<RecentEmployee>(),
                    Departments: departments));

        var viewModel =
            new DashboardViewModel(service);

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
}
