using HrManagement.Application.Organization.Memberships;
using HrManagement.Domain.Organization.Departments;

namespace HrManagement.Desktop.ViewModels;

public sealed record DepartmentStaffingViewItem(
    Department Department,
    OrganizationStaffingCount Staffing)
{
    public Guid Id => Department.Id;

    public string Code => Department.Code;

    public string Name => Department.Name;

    public bool IsActive => Department.IsActive;

    public int ActiveCount => Staffing.ActiveCount;

    public int OnLeaveCount => Staffing.OnLeaveCount;

    public int InactiveCount => Staffing.InactiveCount;

    public int CurrentEmployeeCount =>
        Staffing.CurrentEmployeeCount;

    public int TotalLinkedEmployeeCount =>
        Staffing.TotalLinkedEmployeeCount;

    public string StaffingSummaryText =>
    $"{CurrentEmployeeCount} hiện tại • {InactiveCount} đã nghỉ";

    public string StaffingDetailText =>
    $"Đang làm việc: {ActiveCount} • "
    + $"Nghỉ phép: {OnLeaveCount} • "
    + $"Đã nghỉ: {InactiveCount}";
}
