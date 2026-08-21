namespace HrManagement.Application.Workspaces.AttendanceLeave;

public sealed record AttendanceLeaveEmployeeItem(
    Guid EmployeeId,
    string EmployeeCode,
    string EmployeeName);
