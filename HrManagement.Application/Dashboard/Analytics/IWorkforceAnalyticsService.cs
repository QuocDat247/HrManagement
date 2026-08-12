namespace HrManagement.Application.Dashboard.Analytics;

public interface IWorkforceAnalyticsService
{
    Task<WorkforceMovementSummary> GetWorkforceMovementAsync(
        int year,
        WorkforceAnalyticsGrouping grouping =
            WorkforceAnalyticsGrouping.Monthly,
        CancellationToken cancellationToken = default);
}
