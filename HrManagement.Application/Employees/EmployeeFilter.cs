using HrManagement.Domain.Employees;

namespace HrManagement.Application.Employees;

public sealed record EmployeeFilter(
    string? SearchText = null,
    EmployeeStatus? Status = null,
    bool RequiresProfileCompletionOnly = false);
