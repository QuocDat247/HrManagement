using HrManagement.Domain.Leave.Requests;

namespace HrManagement.Application.Leave.Requests;

public sealed record SubmitLeaveRequestResult(
    bool IsSuccessful,
    Guid? LeaveRequestId = null,
    LeaveRequestStatus? Status = null,
    string? ErrorMessage = null);
