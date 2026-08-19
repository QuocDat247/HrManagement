namespace HrManagement.Domain.Attendance.Schedules;

public sealed class WorkScheduleDay
{
    public Guid Id
    {
        get;
    }

    public Guid WorkScheduleId
    {
        get;
    }

    public DayOfWeek DayOfWeek
    {
        get;
    }

    public bool IsWorkingDay
    {
        get;
    }

    public TimeOnly? StartTime
    {
        get;
    }

    public TimeOnly? EndTime
    {
        get;
    }

    public int BreakMinutes
    {
        get;
    }

    public int PlannedMinutes
    {
        get;
    }

    public bool IsOvernight =>
        IsWorkingDay
        && StartTime.HasValue
        && EndTime.HasValue
        && EndTime.Value <
            StartTime.Value;

    public WorkScheduleDay(
        Guid id,
        Guid workScheduleId,
        DayOfWeek dayOfWeek,
        bool isWorkingDay,
        TimeOnly? startTime = null,
        TimeOnly? endTime = null,
        int breakMinutes = 0)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException(
                "Mã ngày làm việc không hợp lệ.",
                nameof(id));
        }

        if (workScheduleId == Guid.Empty)
        {
            throw new ArgumentException(
                "Mã lịch làm việc không hợp lệ.",
                nameof(workScheduleId));
        }

        if (!Enum.IsDefined(
                dayOfWeek))
        {
            throw new ArgumentOutOfRangeException(
                nameof(dayOfWeek),
                "Ngày trong tuần không hợp lệ.");
        }

        if (breakMinutes < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(breakMinutes),
                "Thời gian nghỉ không được âm.");
        }

        if (!isWorkingDay)
        {
            if (startTime.HasValue
                || endTime.HasValue
                || breakMinutes != 0)
            {
                throw new ArgumentException(
                    "Ngày không làm việc không được có giờ làm hoặc thời gian nghỉ.");
            }

            Id =
                id;

            WorkScheduleId =
                workScheduleId;

            DayOfWeek =
                dayOfWeek;

            IsWorkingDay =
                false;

            StartTime =
                null;

            EndTime =
                null;

            BreakMinutes =
                0;

            PlannedMinutes =
                0;

            return;
        }

        if (!startTime.HasValue
            || !endTime.HasValue)
        {
            throw new ArgumentException(
                "Ngày làm việc phải có giờ bắt đầu và giờ kết thúc.");
        }

        EnsureMinutePrecision(
            startTime.Value,
            nameof(startTime));

        EnsureMinutePrecision(
            endTime.Value,
            nameof(endTime));

        if (startTime.Value ==
            endTime.Value)
        {
            throw new ArgumentException(
                "Giờ bắt đầu và giờ kết thúc không được trùng nhau.");
        }

        int shiftMinutes =
            CalculateShiftMinutes(
                startTime.Value,
                endTime.Value);

        if (breakMinutes >=
            shiftMinutes)
        {
            throw new ArgumentException(
                "Thời gian nghỉ phải ngắn hơn thời lượng ca làm việc.",
                nameof(breakMinutes));
        }

        Id =
            id;

        WorkScheduleId =
            workScheduleId;

        DayOfWeek =
            dayOfWeek;

        IsWorkingDay =
            true;

        StartTime =
            startTime;

        EndTime =
            endTime;

        BreakMinutes =
            breakMinutes;

        PlannedMinutes =
            shiftMinutes
            - breakMinutes;
    }

    private static int CalculateShiftMinutes(
        TimeOnly startTime,
        TimeOnly endTime)
    {
        int startMinutes =
            startTime.Hour * 60
            + startTime.Minute;

        int endMinutes =
            endTime.Hour * 60
            + endTime.Minute;

        int result =
            endMinutes
            - startMinutes;

        if (result < 0)
        {
            result +=
                24 * 60;
        }

        return result;
    }

    private static void EnsureMinutePrecision(
        TimeOnly value,
        string parameterName)
    {
        if (value.Ticks %
            TimeSpan.TicksPerMinute != 0)
        {
            throw new ArgumentException(
                "Giờ làm việc phải có độ chính xác theo phút.",
                parameterName);
        }
    }
}
