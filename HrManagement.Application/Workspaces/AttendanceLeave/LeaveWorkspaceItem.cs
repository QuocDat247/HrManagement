using HrManagement.Domain.Leave.Requests;

namespace HrManagement.Application.Workspaces.AttendanceLeave;

public sealed record LeaveWorkspaceItem(
    Guid LeaveRequestId,
    Guid EmployeeId,
    string EmployeeCode,
    string EmployeeName,
    Guid LeaveTypeId,
    string LeaveTypeCode,
    string LeaveTypeName,
    bool IsPaid,
    DateOnly StartDate,
    DateOnly EndDate,
    LeaveRequestStatus Status,
    DateTime SubmittedAtUtc,
    string? Reason);
