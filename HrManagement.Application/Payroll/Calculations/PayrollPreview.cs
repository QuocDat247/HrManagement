namespace HrManagement.Application.Payroll.Calculations;

public sealed record PayrollPreview(
    int Year,
    int Month,
    Guid? TimesheetPeriodId,
    IReadOnlyList<PayrollEmployeePreview> Employees,
    IReadOnlyList<PayrollCalculationIssue> Issues)
{
    public bool IsFinalizable =>
        TimesheetPeriodId.HasValue
        && Issues.Count == 0
        && Employees.All(
            employee =>
                employee.IsFinalizable);
}
