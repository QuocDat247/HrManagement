namespace HrManagement.Application.Workspaces.AttendanceLeave;

public sealed record LeaveTypeWorkspaceOption(
    Guid LeaveTypeId,
    string Code,
    string Name,
    bool IsPaid);
