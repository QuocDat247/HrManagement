namespace HrManagement.Application.Payroll.Compensation;

public sealed record SetEmployeeCompensationResult(
    bool IsSuccessful,
    Guid? CompensationId = null,
    Guid? PreviousCompensationId = null,
    string? ErrorMessage = null);
