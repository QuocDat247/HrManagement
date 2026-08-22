namespace HrManagement.Application.Attendance.Schedules;

public sealed record CloneWorkScheduleRequest(
    Guid SourceWorkScheduleId,
    string Code,
    string Name);
