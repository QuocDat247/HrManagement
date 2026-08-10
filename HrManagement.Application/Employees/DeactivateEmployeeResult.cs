namespace HrManagement.Application.Employees;

public sealed record DeactivateEmployeeResult(
    bool IsSuccessful,
    string? ErrorMessage = null);
