namespace HrManagement.Desktop.ViewModels;

public sealed record PayrollCurrencySummaryViewModel(
    string CurrencyCode,
    int EmployeeCount,
    decimal BaseSalaryAmount,
    decimal? OvertimeAmount,
    decimal? GrossAmount,
    bool IsComplete);
