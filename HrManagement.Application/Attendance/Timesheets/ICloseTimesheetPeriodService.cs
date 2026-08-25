namespace HrManagement.Application.Attendance.Timesheets;

public interface ICloseTimesheetPeriodService
{
    Task<CloseTimesheetPeriodResult> CloseAsync(
        CloseTimesheetPeriodRequest request,
        CancellationToken cancellationToken = default);
}
