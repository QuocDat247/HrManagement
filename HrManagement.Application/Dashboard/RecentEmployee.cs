using HrManagement.Domain.Employees;

namespace HrManagement.Application.Dashboard;

public sealed record RecentEmployee(
    Guid Id,
    string EmployeeCode,
    string FullName,
    string Department,
    string Position,
    DateOnly HireDate,
    EmployeeStatus Status);
