using HrManagement.Domain.Overtime.Requests;

namespace HrManagement.Application.Overtime.Requests;

public sealed record SubmitOvertimeRequestResult(
    bool IsSuccessful,
    Guid? OvertimeRequestId = null,
    OvertimeRequestStatus? Status = null,
    string? ErrorMessage = null);
