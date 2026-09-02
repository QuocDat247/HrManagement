namespace HrManagement.Application.Payroll.Periods;

public sealed record ClosedPayrollCurrencySummary(
    string CurrencyCode,
    int EmployeeCount,
    decimal BaseSalaryAmount,
    decimal OvertimeAmount,
    decimal GrossAmount);
