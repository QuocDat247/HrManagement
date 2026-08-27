namespace HrManagement.Application.Workspaces.Overtime;

public sealed record OvertimeEmployeeOption(
    Guid EmployeeId,
    string EmployeeCode,
    string EmployeeName);
