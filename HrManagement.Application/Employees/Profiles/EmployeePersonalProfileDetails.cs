using HrManagement.Domain.Employees.Profiles;

namespace HrManagement.Application.Employees.Profiles;

public sealed record EmployeePersonalProfileDetails(
    Guid EmployeeId,
    bool HasProfile,
    string? PreferredName,
    EmployeeGender? Gender,
    string? Nationality,
    string? PlaceOfBirth);
