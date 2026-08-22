namespace HrManagement.Application.Attendance.Schedules;

public sealed record UpdateWorkScheduleRequest(
    Guid WorkScheduleId,
    string Code,
    string Name,
    string TimeZoneId);
