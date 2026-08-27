using HrManagement.Application.Authentication;

namespace HrManagement.Application.Overtime.Requests;

public sealed record OvertimeRequestSubmissionAuthorizationRequest(
    AuthenticatedUser Actor,
    Guid EmployeeId,
    DateOnly WorkDate);
