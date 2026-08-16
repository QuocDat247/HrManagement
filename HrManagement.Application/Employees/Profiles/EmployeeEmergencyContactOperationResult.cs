namespace HrManagement.Application.Employees.Profiles;

public sealed record EmployeeEmergencyContactOperationResult(
    bool IsSuccessful,
    string? ErrorMessage = null,
    Guid? ContactId = null);
