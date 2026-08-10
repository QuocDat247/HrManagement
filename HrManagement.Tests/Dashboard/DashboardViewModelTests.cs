using HrManagement.Application.Dashboard;
using HrManagement.Desktop.ViewModels;

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
                InactiveEmployees: 3));

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
}
