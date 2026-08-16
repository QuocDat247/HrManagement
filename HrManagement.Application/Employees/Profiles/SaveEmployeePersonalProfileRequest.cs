using HrManagement.Domain.Employees.Profiles;

namespace HrManagement.Application.Employees.Profiles;

public sealed record SaveEmployeePersonalProfileRequest(
    Guid EmployeeId,
    string? PreferredName,
    EmployeeGender? Gender,
    string? Nationality,
    string? PlaceOfBirth);
