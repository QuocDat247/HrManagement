using HrManagement.Application.Authentication;

namespace HrManagement.Application.Payroll.Periods;

public sealed record PayrollPeriodClosingAuthorizationRequest(
    AuthenticatedUser Actor,
    int Year,
    int Month);
