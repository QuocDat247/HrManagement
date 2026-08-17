using HrManagement.Domain.Employees.Profiles;

namespace HrManagement.Desktop.ViewModels;

public sealed record EmployeeIdentificationTypeOption(
    EmployeeIdentificationType Value,
    string DisplayName);
