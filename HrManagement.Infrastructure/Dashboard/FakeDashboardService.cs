using HrManagement.Application.Dashboard;

namespace HrManagement.Infrastructure.Dashboard;

public sealed class FakeDashboardService : IDashboardService
{
    public Task<DashboardSummary> GetSummaryAsync(
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var summary = new DashboardSummary(
            TotalEmployees: 128,
            ActiveEmployees: 119,
            EmployeesOnLeave: 6,
            ContractsExpiringSoon: 4);

        return Task.FromResult(summary);
    }
}
