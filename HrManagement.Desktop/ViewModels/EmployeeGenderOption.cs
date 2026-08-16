using HrManagement.Domain.Employees.Profiles;

namespace HrManagement.Desktop.ViewModels;

public sealed record EmployeeGenderOption(
    EmployeeGender Value,
    string DisplayName);
