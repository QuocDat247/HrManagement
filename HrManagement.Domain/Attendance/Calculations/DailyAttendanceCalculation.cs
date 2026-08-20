namespace HrManagement.Domain.Attendance.Calculations;

public sealed class DailyAttendanceCalculation
{
    public AttendanceCalculationStatus Status
    {
        get;
    }

    public int WorkedMinutes
    {
        get;
    }

    public int CompletedPairCount
    {
        get;
    }

    public DateTime? FirstClockInAtUtc
    {
        get;
    }

    public DateTime? LastClockOutAtUtc
    {
        get;
    }

    public bool HasOpenClockIn
    {
        get;
    }

    internal DailyAttendanceCalculation(
        AttendanceCalculationStatus status,
        int workedMinutes,
        int completedPairCount,
        DateTime? firstClockInAtUtc,
        DateTime? lastClockOutAtUtc,
        bool hasOpenClockIn)
    {
        Status =
            status;

        WorkedMinutes =
            workedMinutes;

        CompletedPairCount =
            completedPairCount;

        FirstClockInAtUtc =
            firstClockInAtUtc;

        LastClockOutAtUtc =
            lastClockOutAtUtc;

        HasOpenClockIn =
            hasOpenClockIn;
    }
}
