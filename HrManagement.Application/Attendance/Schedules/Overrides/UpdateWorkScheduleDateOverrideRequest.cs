namespace HrManagement.Application.Attendance.Schedules.Overrides;

public sealed record UpdateWorkScheduleDateOverrideRequest(
    Guid WorkScheduleDateOverrideId,
    bool IsWorkingDay,
    TimeOnly? StartTime = null,
    TimeOnly? EndTime = null,
    int BreakMinutes = 0,
    string? Note = null);
