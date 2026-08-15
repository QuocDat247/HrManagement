using HrManagement.Domain.Employees;

namespace HrManagement.Application.Organization.Memberships;

public sealed record OrganizationEmployeeListItem(
    Guid EmployeeId,
    string EmployeeCode,
    string FullName,
    string DepartmentName,
    string PositionName,
    EmployeeStatus Status,
    DateOnly HireDate);
