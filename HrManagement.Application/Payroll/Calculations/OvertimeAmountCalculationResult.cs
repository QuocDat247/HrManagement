namespace HrManagement.Application.Payroll.Calculations;

public sealed record OvertimeAmountCalculationResult(
    bool IsCalculated,
    decimal? Amount = null,
    string? ErrorMessage = null);
