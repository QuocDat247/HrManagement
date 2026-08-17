namespace HrManagement.Application.Employees.Profiles;

public sealed record EmployeeIdentificationRecordOperationResult(
    bool IsSuccessful,
    string? ErrorMessage = null,
    Guid? RecordId = null);
