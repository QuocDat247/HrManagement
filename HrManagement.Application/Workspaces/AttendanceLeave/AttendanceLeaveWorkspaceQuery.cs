namespace HrManagement.Application.Workspaces.AttendanceLeave;

public sealed record AttendanceLeaveWorkspaceQuery(
    DateOnly FromDate,
    DateOnly ToDate,
    Guid? EmployeeId = null);
