namespace HrManagement.Application.Attendance.Timesheets;

public sealed record CloseTimesheetPeriodPersistenceResult(
    Guid TimesheetPeriodId,
    int SnapshotCount);
