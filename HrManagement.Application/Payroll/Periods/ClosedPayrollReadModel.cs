namespace HrManagement.Application.Payroll.Periods;

public sealed record ClosedPayrollReadModel(
    Guid PayrollPeriodId,
    Guid TimesheetPeriodId,
    int Year,
    int Month,
    DateTime ClosedAtUtc,
    string ClosedByUserId,
    string ClosedByUsername,
    IReadOnlyList<ClosedPayrollEmployeeItem> Employees,
    IReadOnlyList<ClosedPayrollCurrencySummary> CurrencySummaries)
{
    public int SnapshotCount =>
        Employees.Count;
}
