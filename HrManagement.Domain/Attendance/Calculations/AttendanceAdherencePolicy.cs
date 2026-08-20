namespace HrManagement.Domain.Attendance.Calculations;

public sealed class AttendanceAdherencePolicy
{
    public int LateGraceMinutes
    {
        get;
    }

    public int EarlyLeaveGraceMinutes
    {
        get;
    }

    public AttendanceMinuteRoundingMode RoundingMode
    {
        get;
    }

    public AttendanceAdherencePolicy(
        int lateGraceMinutes = 0,
        int earlyLeaveGraceMinutes = 0,
        AttendanceMinuteRoundingMode roundingMode =
            AttendanceMinuteRoundingMode.Floor)
    {
        if (lateGraceMinutes < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(lateGraceMinutes),
                "Thời gian miễn trễ không được âm.");
        }

        if (earlyLeaveGraceMinutes < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(earlyLeaveGraceMinutes),
                "Thời gian miễn về sớm không được âm.");
        }

        if (!Enum.IsDefined(
                roundingMode))
        {
            throw new ArgumentOutOfRangeException(
                nameof(roundingMode),
                "Quy tắc làm tròn thời gian không hợp lệ.");
        }

        LateGraceMinutes =
            lateGraceMinutes;

        EarlyLeaveGraceMinutes =
            earlyLeaveGraceMinutes;

        RoundingMode =
            roundingMode;
    }
}
