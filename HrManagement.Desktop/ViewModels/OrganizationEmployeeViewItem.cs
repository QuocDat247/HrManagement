namespace HrManagement.Desktop.ViewModels;

public sealed record OrganizationEmployeeViewItem(
    Guid EmployeeId,
    string EmployeeCode,
    string FullName,
    string DepartmentName,
    string PositionName,
    string StatusText,
    string HireDateText);
