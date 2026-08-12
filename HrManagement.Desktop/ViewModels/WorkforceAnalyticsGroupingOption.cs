using HrManagement.Application.Dashboard.Analytics;

namespace HrManagement.Desktop.ViewModels;

public sealed record WorkforceAnalyticsGroupingOption(
    string DisplayName,
    WorkforceAnalyticsGrouping Grouping);
