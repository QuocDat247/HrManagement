namespace HrManagement.Application.Payroll.Calculations;

public sealed record OvertimePayabilityResolution(
    Guid OvertimeRequestId,
    Guid EmployeeId,
    DateOnly WorkDate,
    int ApprovedMinutes,
    int? PayableMinutes,
    OvertimePayabilityStatus Status,
    string Reason)
{
    public bool IsResolved =>
        Status is
            OvertimePayabilityStatus.Payable
            or OvertimePayabilityStatus.NotPayable;
}
