using HrManagement.Application.Attendance.Timesheets;

namespace HrManagement.Tests.Attendance;

internal sealed class StubAttendancePeriodLockPolicy
    : IAttendancePeriodLockPolicy
{
    public bool IsLocked
    {
        get;
        set;
    }

    public int CallCount
    {
        get;
        private set;
    }

    public DateOnly? LastWorkDate
    {
        get;
        private set;
    }

    public Task<bool> IsLockedAsync(
        DateOnly workDate,
        CancellationToken cancellationToken = default)
    {
        CallCount++;

        LastWorkDate =
            workDate;

        return Task.FromResult(
            IsLocked);
    }
}
