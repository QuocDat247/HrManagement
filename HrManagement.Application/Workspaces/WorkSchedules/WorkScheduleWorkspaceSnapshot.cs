namespace HrManagement.Application.Workspaces.WorkSchedules;

public sealed record WorkScheduleWorkspaceSnapshot(
    IReadOnlyList<WorkScheduleWorkspaceScheduleItem> Schedules,
    IReadOnlyList<WorkScheduleWorkspaceDayItem> ScheduleDays,
    IReadOnlyList<WorkScheduleWorkspaceAssignmentItem> Assignments);
