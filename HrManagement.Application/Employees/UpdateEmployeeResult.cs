namespace HrManagement.Application.Employees;

public sealed record UpdateEmployeeResult(
    bool IsSuccessful,
    string? ErrorMessage = null);
