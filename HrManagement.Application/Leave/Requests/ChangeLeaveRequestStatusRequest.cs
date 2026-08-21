using HrManagement.Domain.Leave.Requests;

namespace HrManagement.Application.Leave.Requests;

public sealed record ChangeLeaveRequestStatusRequest(
    Guid LeaveRequestId,
    LeaveRequestStatus TargetStatus,
    string? Note = null);
