using HrManagement.Domain.Overtime.Requests;

namespace HrManagement.Application.Overtime.Requests;

public sealed record ChangeOvertimeRequestStatusRequest(
    Guid OvertimeRequestId,
    OvertimeRequestStatus ExpectedStatus,
    OvertimeRequestStatus TargetStatus,
    int? ApprovedMinutes = null,
    string? Note = null);
