namespace HrManagement.Application.Attendance.Timesheets;

public interface ICloseTimesheetPeriodPersistence
{
    Task<CloseTimesheetPeriodPersistenceResult> CloseAsync(
        int year,
        int month,
        DateTime closedAtUtc,
        string actorUserId,
        string actorUsername,
        CancellationToken cancellationToken = default);
}
