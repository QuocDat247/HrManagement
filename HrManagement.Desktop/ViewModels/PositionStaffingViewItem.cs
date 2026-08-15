using HrManagement.Application.Organization.Memberships;
using HrManagement.Domain.Organization.Positions;

namespace HrManagement.Desktop.ViewModels;

public sealed record PositionStaffingViewItem(
    Position Position,
    OrganizationStaffingCount Staffing)
{
    public Guid Id => Position.Id;

    public string Code => Position.Code;

    public string Name => Position.Name;

    public bool IsActive => Position.IsActive;

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
