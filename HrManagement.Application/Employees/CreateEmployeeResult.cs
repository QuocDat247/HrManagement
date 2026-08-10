namespace HrManagement.Application.Employees;

public sealed record CreateEmployeeResult(
    bool IsSuccessful,
    string? ErrorMessage = null,
    Guid? EmployeeId = null);
