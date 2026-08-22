namespace HrManagement.Application.Workspaces.WorkSchedules;

public sealed record WorkScheduleWorkspaceEmployeeItem(
    Guid EmployeeId,
    string EmployeeCode,
    string EmployeeName);
