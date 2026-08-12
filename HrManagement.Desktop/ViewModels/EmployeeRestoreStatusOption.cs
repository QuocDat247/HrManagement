using HrManagement.Domain.Employees;

namespace HrManagement.Desktop.ViewModels;

public sealed record EmployeeRestoreStatusOption(
    string DisplayName,
    EmployeeStatus Status);
