namespace HrManagement.Application.Dashboard.Analytics;

public sealed record WorkforceMovementPeriod(
    int PeriodNumber,
    DateOnly StartDate,
    DateOnly EndDate,
    int NewHires,
    int Separations,
    int BeginningHeadcount,
    int EndingHeadcount,
    decimal AverageHeadcount,
    decimal TurnoverRate,
    int NetChange);
