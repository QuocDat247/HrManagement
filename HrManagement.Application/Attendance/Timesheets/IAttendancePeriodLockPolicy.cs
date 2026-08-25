namespace HrManagement.Application.Attendance.Timesheets;

public interface IAttendancePeriodLockPolicy
{
    Task<bool> IsLockedAsync(
        DateOnly workDate,
        CancellationToken cancellationToken = default);
}
