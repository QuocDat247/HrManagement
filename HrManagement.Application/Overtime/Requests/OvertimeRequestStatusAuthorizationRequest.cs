using HrManagement.Application.Authentication;
using HrManagement.Domain.Overtime.Requests;

namespace HrManagement.Application.Overtime.Requests;

public sealed record OvertimeRequestStatusAuthorizationRequest(
    AuthenticatedUser Actor,
    Guid OvertimeRequestId,
    OvertimeRequestStatus TargetStatus);
