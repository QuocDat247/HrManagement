namespace HrManagement.Application.Workspaces.WorkSchedules;

public sealed record WorkScheduleWorkspaceScheduleItem(
    Guid WorkScheduleId,
    string Code,
    string Name,
    string TimeZoneId,
    bool IsActive);
