using HrManagement.Domain.Attendance.Calculations;

namespace HrManagement.Application.Workspaces.AttendanceLeave;

public sealed record AttendanceWorkspaceItem(
    Guid AttendanceRecordId,
    Guid EmployeeId,
    string EmployeeCode,
    string EmployeeName,
    DateOnly WorkDate,
    bool IsWorkingDay,
    TimeOnly? ExpectedStartTime,
    TimeOnly? ExpectedEndTime,
    AttendanceCalculationStatus Status,
    int WorkedMinutes,
    int LateMinutes,
    int EarlyLeaveMinutes);
