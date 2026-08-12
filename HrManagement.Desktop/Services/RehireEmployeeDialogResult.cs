using HrManagement.Domain.Employees;

namespace HrManagement.Desktop.Services;

public sealed record RehireEmployeeDialogResult(
    DateOnly RehireDate,
    EmployeeStatus RehireStatus);
