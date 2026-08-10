using HrManagement.Domain.Employees;

namespace HrManagement.Application.Employees;

public sealed record UpdateEmployeeRequest(
    Guid Id,
    string EmployeeCode,
    string FullName,
    string? Email,
    string? PhoneNumber,
    DateOnly? DateOfBirth,
    DateOnly HireDate,
    string Department,
    string Position,
    EmployeeStatus Status);
