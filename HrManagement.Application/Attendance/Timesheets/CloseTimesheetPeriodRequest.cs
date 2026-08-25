namespace HrManagement.Application.Attendance.Timesheets;

public sealed record CloseTimesheetPeriodRequest(
    int Year,
    int Month);
