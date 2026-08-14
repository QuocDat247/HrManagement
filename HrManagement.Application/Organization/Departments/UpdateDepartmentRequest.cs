namespace HrManagement.Application.Organization.Departments;

public sealed record UpdateDepartmentRequest(
    Guid DepartmentId,
    string Code,
    string Name);
