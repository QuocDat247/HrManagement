namespace HrManagement.Application.Employees;

public sealed record CancelEmployeeDeactivationResult(
    bool IsSuccessful,
    string? ErrorMessage);
