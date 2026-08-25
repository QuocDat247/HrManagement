namespace HrManagement.Application.Attendance.Timesheets;

public sealed record CloseTimesheetPeriodResult(
    bool IsSuccessful,
    Guid? TimesheetPeriodId = null,
    int? SnapshotCount = null,
    DateTime? ClosedAtUtc = null,
    string? ErrorMessage = null);
