using HrManagement.Domain.Overtime.Requests;

namespace HrManagement.Application.Overtime.Requests;

public sealed record ChangeOvertimeRequestStatusResult(
    bool IsSuccessful,
    Guid? OvertimeRequestId = null,
    OvertimeRequestStatus? Status = null,
    int? ApprovedMinutes = null,
    Guid? StatusChangeId = null,
    string? ErrorMessage = null);
