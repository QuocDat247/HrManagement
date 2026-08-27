using HrManagement.Application.Workspaces.Overtime;
using HrManagement.Domain.Overtime.Requests;

namespace HrManagement.Desktop.ViewModels;

public sealed record OvertimeWorkspaceRowViewModel(
    Guid OvertimeRequestId,
    Guid EmployeeId,
    string EmployeeCode,
    string EmployeeName,
    DateOnly WorkDate,
    int RequestedMinutes,
    int? ApprovedMinutes,
    OvertimeRequestStatus Status,
    DateTime SubmittedAtUtc,
    string? Reason)
{
    public string WorkDateText =>
        WorkDate.ToString(
            "dd/MM/yyyy");

    public string StatusText =>
        OvertimeStatusText.Get(
            Status);

    public string ApprovedMinutesText =>
        ApprovedMinutes.HasValue
            ? ApprovedMinutes.Value.ToString()
            : "—";

    public string ReasonText =>
        string.IsNullOrWhiteSpace(
            Reason)
            ? "—"
            : Reason;

    public static OvertimeWorkspaceRowViewModel From(
        OvertimeWorkspaceItem item)
    {
        ArgumentNullException.ThrowIfNull(
            item);

        return new OvertimeWorkspaceRowViewModel(
            item.OvertimeRequestId,
            item.EmployeeId,
            item.EmployeeCode,
            item.EmployeeName,
            item.WorkDate,
            item.RequestedMinutes,
            item.ApprovedMinutes,
            item.Status,
            item.SubmittedAtUtc,
            item.Reason);
    }
}
