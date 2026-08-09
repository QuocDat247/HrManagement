namespace HrManagement.Application.Dashboard;

public sealed record DashboardSummary(
    int TotalEmployees,
    int ActiveEmployees,
    int EmployeesOnLeave,
    int ContractsExpiringSoon);
