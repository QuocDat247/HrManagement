using HrManagement.Domain.Employees.Profiles;

namespace HrManagement.Application.Employees.Profiles;

public sealed record SaveEmployeeIdentificationRecordRequest(
    Guid EmployeeId,
    Guid? RecordId,
    EmployeeIdentificationType Type,
    string DocumentNumber,
    DateOnly? IssueDate,
    DateOnly? ExpiryDate,
    string? IssuingAuthority,
    string? PlaceOfIssue,
    string? IssuingCountry);
