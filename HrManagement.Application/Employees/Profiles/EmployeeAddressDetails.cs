using HrManagement.Domain.Employees.Profiles;

namespace HrManagement.Application.Employees.Profiles;

public sealed record EmployeeAddressDetails(
    Guid Id,
    EmployeeAddressType Type,
    string AddressLine,
    string? Ward,
    string? District,
    string? Province,
    string Country,
    string? PostalCode);
