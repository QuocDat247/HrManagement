using HrManagement.Domain.Attendance.Records;

namespace HrManagement.Domain.Attendance.Calculations;

public static class AttendanceScheduleAdherenceCalculator
{
    public static AttendanceScheduleAdherence Calculate(
        AttendanceRecord record,
        DailyAttendanceCalculation dailyCalculation,
        AttendanceScheduleWindow? scheduleWindow,
        AttendanceAdherencePolicy policy)
    {
        ArgumentNullException.ThrowIfNull(
            record);

        ArgumentNullException.ThrowIfNull(
            dailyCalculation);

        ArgumentNullException.ThrowIfNull(
            policy);

        if (!record.IsWorkingDay)
        {
            return Zero();
        }

        if (dailyCalculation.Status ==
            AttendanceCalculationStatus.Absent)
        {
            return Zero();
        }

        if (!dailyCalculation.FirstClockInAtUtc.HasValue)
        {
            return Zero();
        }

        if (scheduleWindow is null)
        {
            throw new ArgumentNullException(
                nameof(scheduleWindow),
                "Ngày làm việc có punch phải có khung giờ dự kiến.");
        }

        int lateMinutes =
            CalculateLateMinutes(
                dailyCalculation.FirstClockInAtUtc.Value,
                scheduleWindow.ExpectedStartAtUtc,
                policy);

        int earlyLeaveMinutes =
            CalculateEarlyLeaveMinutes(
                dailyCalculation,
                scheduleWindow,
                policy);

        return new AttendanceScheduleAdherence(
            lateMinutes,
            earlyLeaveMinutes);
    }

    private static int CalculateLateMinutes(
        DateTime firstClockInAtUtc,
        DateTime expectedStartAtUtc,
        AttendanceAdherencePolicy policy)
    {
        if (firstClockInAtUtc <=
            expectedStartAtUtc)
        {
            return 0;
        }

        TimeSpan lateness =
            firstClockInAtUtc
            - expectedStartAtUtc;

        int roundedMinutes =
            RoundPositiveDuration(
                lateness,
                policy.RoundingMode);

        return Math.Max(
            0,
            roundedMinutes
            - policy.LateGraceMinutes);
    }

    private static int CalculateEarlyLeaveMinutes(
        DailyAttendanceCalculation dailyCalculation,
        AttendanceScheduleWindow scheduleWindow,
        AttendanceAdherencePolicy policy)
    {
        if (dailyCalculation.HasOpenClockIn)
        {
            return 0;
        }

        if (!dailyCalculation.LastClockOutAtUtc.HasValue)
        {
            return 0;
        }

        DateTime lastClockOutAtUtc =
            dailyCalculation.LastClockOutAtUtc.Value;

        if (lastClockOutAtUtc >=
            scheduleWindow.ExpectedEndAtUtc)
        {
            return 0;
        }

        TimeSpan earlyLeave =
            scheduleWindow.ExpectedEndAtUtc
            - lastClockOutAtUtc;

        int roundedMinutes =
            RoundPositiveDuration(
                earlyLeave,
                policy.RoundingMode);

        return Math.Max(
            0,
            roundedMinutes
            - policy.EarlyLeaveGraceMinutes);
    }

    private static int RoundPositiveDuration(
        TimeSpan duration,
        AttendanceMinuteRoundingMode roundingMode)
    {
        if (duration <=
            TimeSpan.Zero)
        {
            return 0;
        }

        long ticks =
            duration.Ticks;

        long minutes =
            roundingMode switch
            {
                AttendanceMinuteRoundingMode.Floor =>
                    ticks /
                    TimeSpan.TicksPerMinute,

                AttendanceMinuteRoundingMode.Ceiling =>
                    (
                        ticks
                        + TimeSpan.TicksPerMinute
                        - 1
                    ) /
                    TimeSpan.TicksPerMinute,

                _ =>
                    throw new ArgumentOutOfRangeException(
                        nameof(roundingMode))
            };

        return checked(
            (int)minutes);
    }

    private static AttendanceScheduleAdherence Zero()
    {
        return new AttendanceScheduleAdherence(
            lateMinutes: 0,
            earlyLeaveMinutes: 0);
    }
}
