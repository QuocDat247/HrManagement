namespace HrManagement.Application.Payroll.Calculations;

public sealed record BaseSalaryProrationResult(
    string CurrencyCode,
    decimal TotalAmount,
    IReadOnlyList<BaseSalaryProrationComponent> Components);
