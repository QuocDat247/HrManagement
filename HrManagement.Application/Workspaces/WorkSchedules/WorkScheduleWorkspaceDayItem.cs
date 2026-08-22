namespace HrManagement.Application.Workspaces.WorkSchedules;

public sealed record WorkScheduleWorkspaceDayItem(
    Guid WorkScheduleDayId,
    Guid WorkScheduleId,
    DayOfWeek DayOfWeek,
    bool IsWorkingDay,
    TimeOnly? StartTime,
    TimeOnly? EndTime,
    int BreakMinutes,
    int PlannedMinutes);
