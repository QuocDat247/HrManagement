namespace HrManagement.Application.Organization.Departments;

public sealed record CreateDepartmentRequest(
    string Code,
    string Name);
