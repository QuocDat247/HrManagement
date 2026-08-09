using HrManagement.Domain.Employees;

namespace HrManagement.Desktop.ViewModels;

public sealed record EmployeeStatusFilterOption(
    string DisplayName,
    EmployeeStatus? Status);
