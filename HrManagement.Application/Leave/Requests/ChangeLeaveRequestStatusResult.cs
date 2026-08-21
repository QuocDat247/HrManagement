using HrManagement.Domain.Leave.Requests;

namespace HrManagement.Application.Leave.Requests;

public sealed record ChangeLeaveRequestStatusResult(
    bool IsSuccessful,
    Guid? LeaveRequestId = null,
    LeaveRequestStatus? Status = null,
    Guid? StatusChangeId = null,
    string? ErrorMessage = null);
