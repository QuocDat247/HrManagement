namespace HrManagement.Application.Payroll.Calculations;

public sealed record PayrollCalculationInput(
    int Year,
    int Month,
    Guid? TimesheetPeriodId,
    bool IsTimesheetClosed,
    IReadOnlyList<PayrollEmployeeCalculationInput> Employees,
    IReadOnlyList<PayrollCalculationIssue> Issues)
{
    public bool IsStructurallyReady =>
        IsTimesheetClosed
        && TimesheetPeriodId.HasValue
        && Issues.Count == 0;
}
