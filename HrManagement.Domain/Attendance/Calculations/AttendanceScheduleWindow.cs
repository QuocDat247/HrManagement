namespace HrManagement.Domain.Attendance.Calculations;

public sealed class AttendanceScheduleWindow
{
    public DateTime ExpectedStartAtUtc
    {
        get;
    }

    public DateTime ExpectedEndAtUtc
    {
        get;
    }

    public AttendanceScheduleWindow(
        DateTime expectedStartAtUtc,
        DateTime expectedEndAtUtc)
    {
        EnsureUtc(
            expectedStartAtUtc,
            nameof(expectedStartAtUtc));

        EnsureUtc(
            expectedEndAtUtc,
            nameof(expectedEndAtUtc));

        if (expectedEndAtUtc <=
            expectedStartAtUtc)
        {
            throw new ArgumentException(
                "Thời điểm kết thúc dự kiến phải sau thời điểm bắt đầu dự kiến.",
                nameof(expectedEndAtUtc));
        }

        ExpectedStartAtUtc =
            expectedStartAtUtc;

        ExpectedEndAtUtc =
            expectedEndAtUtc;
    }

    private static void EnsureUtc(
        DateTime value,
        string parameterName)
    {
        if (value == default)
        {
            throw new ArgumentException(
                "Thời điểm dự kiến không hợp lệ.",
                parameterName);
        }

        if (value.Kind !=
            DateTimeKind.Utc)
        {
            throw new ArgumentException(
                "Thời điểm dự kiến phải ở UTC.",
                parameterName);
        }
    }
}
