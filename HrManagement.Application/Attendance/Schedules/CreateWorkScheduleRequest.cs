namespace HrManagement.Application.Attendance.Schedules;

public sealed record CreateWorkScheduleRequest(
    string Code,
    string Name,
    string TimeZoneId);
