using HrManagement.Domain.Employees;

namespace HrManagement.Application.Employees;

public sealed record UpdateEmployeeRequest(
    Guid EmployeeId,
    string EmployeeCode,
    string FullName,
    string? Email,
    string? PhoneNumber,
    DateOnly? DateOfBirth,
    DateOnly HireDate,
    Guid DepartmentId,
    Guid PositionId,
    EmployeeStatus Status);
