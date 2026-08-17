using HrManagement.Domain.Employees.Profiles;

namespace HrManagement.Application.Employees.Profiles;

public sealed record EmployeeIdentificationRecordDetails(
    Guid Id,
    EmployeeIdentificationType Type,
    string DocumentNumber,
    DateOnly? IssueDate,
    DateOnly? ExpiryDate,
    string? IssuingAuthority,
    string? PlaceOfIssue,
    string? IssuingCountry);
