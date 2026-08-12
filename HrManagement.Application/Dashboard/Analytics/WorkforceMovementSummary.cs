namespace HrManagement.Application.Dashboard.Analytics;

public sealed record WorkforceMovementSummary(
    int Year,
    WorkforceAnalyticsGrouping Grouping,
    int BeginningHeadcount,
    int EndingHeadcount,
    int TotalNewHires,
    int TotalSeparations,
    int NetChange,
    decimal AverageHeadcount,
    decimal TurnoverRate,
    int EmployeesWithUnknownTerminationDate,
    IReadOnlyList<WorkforceMovementPeriod> Periods);
