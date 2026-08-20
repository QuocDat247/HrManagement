namespace HrManagement.Domain.Attendance.Calculations;

public sealed class AttendanceScheduleAdherence
{
    public int LateMinutes
    {
        get;
    }

    public int EarlyLeaveMinutes
    {
        get;
    }

    internal AttendanceScheduleAdherence(
        int lateMinutes,
        int earlyLeaveMinutes)
    {
        LateMinutes =
            lateMinutes;

        EarlyLeaveMinutes =
            earlyLeaveMinutes;
    }
}
