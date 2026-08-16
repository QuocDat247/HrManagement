namespace HrManagement.Application.Employees.Profiles;

public sealed record EmployeeAddressOperationResult(
    bool IsSuccessful,
    string? ErrorMessage = null);
