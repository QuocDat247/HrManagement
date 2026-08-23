namespace HrManagement.Application.Attendance.Schedules.Overrides;

public sealed record CreateWorkScheduleDateOverrideRequest(
    Guid WorkScheduleId,
    DateOnly WorkDate,
    bool IsWorkingDay,
    TimeOnly? StartTime = null,
    TimeOnly? EndTime = null,
    int BreakMinutes = 0,
    string? Note = null);
