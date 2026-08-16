namespace HrManagement.Application.Employees.Profiles;

public sealed record SaveEmployeeEmergencyContactRequest(
    Guid EmployeeId,
    Guid? ContactId,
    string FullName,
    string Relationship,
    string PhoneNumber,
    string? Email,
    bool IsPrimary);
