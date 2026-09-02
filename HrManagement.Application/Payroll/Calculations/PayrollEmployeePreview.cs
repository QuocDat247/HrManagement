namespace HrManagement.Application.Payroll.Calculations;

public sealed record PayrollEmployeePreview(
    Guid EmployeeId,
    string EmployeeCode,
    string EmployeeFullName,
    string CurrencyCode,
    decimal BaseSalaryAmount,
    int ApprovedOvertimeMinutes,
    int? PayableOvertimeMinutes,
    decimal? OvertimeAmount,
    decimal? GrossAmount,
    IReadOnlyList<OvertimePayabilityResolution>
        OvertimeResolutions,
    IReadOnlyList<PayrollCalculationIssue> Issues)
{
    public bool IsFinalizable =>
        GrossAmount.HasValue
        && Issues.Count == 0;
}
