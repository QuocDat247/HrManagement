namespace HrManagement.Application.Workspaces.HolidayExceptions;

public sealed record HolidayExceptionWorkspaceOverrideItem(
    Guid Id,
    Guid WorkScheduleId,
    DateOnly WorkDate,
    bool IsWorkingDay,
    TimeOnly? StartTime,
    TimeOnly? EndTime,
    int BreakMinutes,
    int PlannedMinutes,
    bool IsOvernight,
    string? Note);
