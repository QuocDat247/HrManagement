namespace HrManagement.Desktop.ViewModels;

public sealed record PayrollWorkspaceRowViewModel(
    Guid EmployeeId,
    string EmployeeCode,
    string EmployeeFullName,
    string CurrencyCode,
    decimal BaseSalaryAmount,
    int ApprovedOvertimeMinutes,
    int? PayableOvertimeMinutes,
    decimal? OvertimeAmount,
    decimal? GrossAmount,
    bool IsFinalizable,
    string StatusText,
    string? IssuesText);
