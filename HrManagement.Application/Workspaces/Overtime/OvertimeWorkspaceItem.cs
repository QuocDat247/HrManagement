using HrManagement.Domain.Overtime.Requests;

namespace HrManagement.Application.Workspaces.Overtime;

public sealed record OvertimeWorkspaceItem(
    Guid OvertimeRequestId,
    Guid EmployeeId,
    string EmployeeCode,
    string EmployeeName,
    DateOnly WorkDate,
    int RequestedMinutes,
    int? ApprovedMinutes,
    OvertimeRequestStatus Status,
    DateTime SubmittedAtUtc,
    string? Reason);
