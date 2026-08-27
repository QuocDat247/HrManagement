using HrManagement.Application.Workspaces.Overtime;
using HrManagement.Domain.Overtime.Requests;

namespace HrManagement.Desktop.ViewModels;

public sealed record OvertimeStatusHistoryRowViewModel(
    Guid StatusChangeId,
    Guid OvertimeRequestId,
    OvertimeRequestStatus PreviousStatus,
    OvertimeRequestStatus NewStatus,
    int? ApprovedMinutes,
    DateTime ChangedAtUtc,
    string ChangedAtText,
    string ChangedByUsername,
    string? Note)
{
    public string PreviousStatusText =>
        OvertimeStatusText.Get(
            PreviousStatus);

    public string NewStatusText =>
        OvertimeStatusText.Get(
            NewStatus);

    public string ApprovedMinutesText =>
        ApprovedMinutes.HasValue
            ? ApprovedMinutes.Value.ToString()
            : "—";

    public string NoteText =>
        string.IsNullOrWhiteSpace(
            Note)
            ? "—"
            : Note;

    public static OvertimeStatusHistoryRowViewModel From(
        OvertimeStatusHistoryItem item,
        TimeZoneInfo localTimeZone)
    {
        ArgumentNullException.ThrowIfNull(
            item);

        ArgumentNullException.ThrowIfNull(
            localTimeZone);

        DateTime utcValue =
            DateTime.SpecifyKind(
                item.ChangedAtUtc,
                DateTimeKind.Utc);

        DateTime localValue =
            TimeZoneInfo.ConvertTimeFromUtc(
                utcValue,
                localTimeZone);

        return new OvertimeStatusHistoryRowViewModel(
            item.StatusChangeId,
            item.OvertimeRequestId,
            item.PreviousStatus,
            item.NewStatus,
            item.ApprovedMinutes,
            item.ChangedAtUtc,
            localValue.ToString(
                "dd/MM/yyyy HH:mm"),
            item.ChangedByUsername,
            item.Note);
    }
}
