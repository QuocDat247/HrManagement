namespace HrManagement.Application.Attendance.Calculations;

public sealed record RecalculateAttendanceRequest(
    Guid AttendanceRecordId);
