using HrManagement.Domain.Overtime.Requests;

namespace HrManagement.Application.Workspaces.Overtime;

public sealed record OvertimeStatusHistoryItem(
    Guid StatusChangeId,
    Guid OvertimeRequestId,
    OvertimeRequestStatus PreviousStatus,
    OvertimeRequestStatus NewStatus,
    int? ApprovedMinutes,
    DateTime ChangedAtUtc,
    string ChangedByUsername,
    string? Note);
