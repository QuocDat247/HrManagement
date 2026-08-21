namespace HrManagement.Application.Workspaces.AttendanceLeave;

public sealed record AttendanceLeaveWorkspaceSnapshot(
    IReadOnlyList<AttendanceWorkspaceItem> Attendance,
    IReadOnlyList<LeaveWorkspaceItem> LeaveRequests);
