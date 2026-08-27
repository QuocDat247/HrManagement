using HrManagement.Domain.Attendance.Timesheets;

namespace HrManagement.Application.Attendance.Timesheets;

public sealed record MonthlyTimesheetReadModel(
    int Year,
    int Month,
    Guid? TimesheetPeriodId,
    TimesheetPeriodStatus PeriodStatus,
    IReadOnlyList<MonthlyTimesheetDayItem> Items,
    DateTime? ClosedAtUtc = null,
    string? ClosedByUserId = null,
    string? ClosedByUsername = null)
{
    public bool IsClosed =>
        PeriodStatus ==
        TimesheetPeriodStatus.Closed;
}
