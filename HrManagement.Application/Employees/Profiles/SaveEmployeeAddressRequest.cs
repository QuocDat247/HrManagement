using HrManagement.Domain.Employees.Profiles;

namespace HrManagement.Application.Employees.Profiles;

public sealed record SaveEmployeeAddressRequest(
    Guid EmployeeId,
    EmployeeAddressType Type,
    string AddressLine,
    string? Ward,
    string? District,
    string? Province,
    string Country,
    string? PostalCode);
