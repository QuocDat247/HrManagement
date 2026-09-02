namespace HrManagement.Application.Payroll.Periods;

public sealed record ClosedPayrollEmployeeItem(
    Guid SnapshotId,
    Guid EmployeeId,
    string EmployeeCode,
    string EmployeeFullName,
    string CurrencyCode,
    decimal BaseSalaryAmount,
    int ApprovedOvertimeMinutes,
    int PayableOvertimeMinutes,
    decimal OvertimeAmount,
    decimal GrossAmount);
