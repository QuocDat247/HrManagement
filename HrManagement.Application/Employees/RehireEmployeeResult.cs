namespace HrManagement.Application.Employees;

public sealed record RehireEmployeeResult(
    bool IsSuccessful,
    string? ErrorMessage);
