namespace HrManagement.Application.Workspaces.WorkSchedules;

public sealed record WorkScheduleWorkspaceAssignmentItem(
    Guid AssignmentId,
    Guid EmployeeId,
    string EmployeeCode,
    string EmployeeName,
    Guid EmploymentPeriodId,
    Guid WorkScheduleId,
    string WorkScheduleCode,
    string WorkScheduleName,
    DateOnly EffectiveFrom,
    DateOnly? EffectiveTo,
    bool IsOpen);
