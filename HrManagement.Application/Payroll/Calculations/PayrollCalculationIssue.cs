namespace HrManagement.Application.Payroll.Calculations;

public sealed record PayrollCalculationIssue(
    PayrollCalculationIssueCode Code,
    Guid? EmployeeId,
    string Message);
