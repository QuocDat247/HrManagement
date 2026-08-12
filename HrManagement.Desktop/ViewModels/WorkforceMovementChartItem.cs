namespace HrManagement.Desktop.ViewModels;

public sealed record WorkforceMovementChartItem(
    int PeriodNumber,
    string Label,
    string DisplayName,
    int NewHires,
    int Separations,
    int NetChange,
    decimal TurnoverRate);
