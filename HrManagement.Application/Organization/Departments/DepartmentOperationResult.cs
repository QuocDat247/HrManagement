namespace HrManagement.Application.Organization.Departments;

public sealed record DepartmentOperationResult(
    bool IsSuccessful,
    string? ErrorMessage);
