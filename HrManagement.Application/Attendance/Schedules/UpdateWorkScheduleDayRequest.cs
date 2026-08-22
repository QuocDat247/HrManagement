namespace HrManagement.Application.Attendance.Schedules;

public sealed record UpdateWorkScheduleDayRequest(
    Guid WorkScheduleId,
    DayOfWeek DayOfWeek,
    bool IsWorkingDay,
    TimeOnly? StartTime,
    TimeOnly? EndTime,
    int BreakMinutes);
