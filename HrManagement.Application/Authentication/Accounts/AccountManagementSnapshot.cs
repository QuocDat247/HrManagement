using HrManagement.Domain.Authentication.Accounts;
using HrManagement.Domain.Employees;

namespace HrManagement.Application.Authentication.Accounts;

public sealed record AccountManagementSnapshot(
    IReadOnlyList<AccountManagementAccountItem> Accounts,
    IReadOnlyList<AccountManagementRoleItem> Roles,
    IReadOnlyList<AccountManagementEmployeeItem> Employees);

public sealed record AccountManagementAccountItem(
    Guid AccountId,
    string Username,
    string DisplayName,
    UserAccountKind Kind,
    bool IsActive,
    Guid? EmployeeId,
    string? EmployeeCode,
    string? EmployeeName,
    IReadOnlyList<AccountManagementRoleItem> Roles);

public sealed record AccountManagementRoleItem(
    Guid RoleId,
    string Name,
    string? Description,
    bool IsActive);

public sealed record AccountManagementEmployeeItem(
    Guid EmployeeId,
    string EmployeeCode,
    string FullName,
    EmployeeStatus Status);
