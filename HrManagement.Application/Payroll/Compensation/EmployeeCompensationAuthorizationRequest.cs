using HrManagement.Application.Authentication;

namespace HrManagement.Application.Payroll.Compensation;

public sealed record EmployeeCompensationAuthorizationRequest(
    AuthenticatedUser Actor,
    Guid EmployeeId,
    DateOnly EffectiveFrom);
