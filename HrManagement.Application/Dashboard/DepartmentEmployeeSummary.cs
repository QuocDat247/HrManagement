namespace HrManagement.Application.Dashboard;

public sealed record DepartmentEmployeeSummary(
    string Department,
    int TotalEmployees,
    int ActiveEmployees,
    int EmployeesOnLeave,
    int InactiveEmployees);
