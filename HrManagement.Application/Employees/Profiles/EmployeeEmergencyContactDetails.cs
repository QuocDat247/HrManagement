namespace HrManagement.Application.Employees.Profiles;

public sealed record EmployeeEmergencyContactDetails(
    Guid Id,
    string FullName,
    string Relationship,
    string PhoneNumber,
    string? Email,
    bool IsPrimary);
