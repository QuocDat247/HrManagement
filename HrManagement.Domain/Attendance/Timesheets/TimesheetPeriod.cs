namespace HrManagement.Domain.Attendance.Timesheets;

public sealed class TimesheetPeriod
{
    public Guid Id
    {
        get;
    }

    public int Year
    {
        get;
    }

    public int Month
    {
        get;
    }

    public TimesheetPeriodStatus Status
    {
        get;
        private set;
    }

    public DateTime? ClosedAtUtc
    {
        get;
        private set;
    }

    public string? ClosedByUserId
    {
        get;
        private set;
    }

    public string? ClosedByUsername
    {
        get;
        private set;
    }

    public DateOnly StartDate =>
        new(
            Year,
            Month,
            1);

    public DateOnly EndDate =>
        new(
            Year,
            Month,
            DateTime.DaysInMonth(
                Year,
                Month));

    public bool IsClosed =>
        Status ==
        TimesheetPeriodStatus.Closed;

    public TimesheetPeriod(
        Guid id,
        int year,
        int month)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException(
                "Mã kỳ công không hợp lệ.",
                nameof(id));
        }

        if (year < 2000
            || year > 9999)
        {
            throw new ArgumentOutOfRangeException(
                nameof(year),
                "Năm kỳ công không hợp lệ.");
        }

        if (month < 1
            || month > 12)
        {
            throw new ArgumentOutOfRangeException(
                nameof(month),
                "Tháng kỳ công phải từ 1 đến 12.");
        }

        Id =
            id;

        Year =
            year;

        Month =
            month;

        Status =
            TimesheetPeriodStatus.Open;
    }

    public void Close(
        DateTime closedAtUtc,
        string actorUserId,
        string actorUsername)
    {
        if (IsClosed)
        {
            throw new InvalidOperationException(
                "Kỳ công đã được đóng.");
        }

        if (closedAtUtc == default
            || closedAtUtc.Kind !=
                DateTimeKind.Utc)
        {
            throw new ArgumentException(
                "Thời điểm đóng kỳ công phải sử dụng UTC.",
                nameof(closedAtUtc));
        }

        if (string.IsNullOrWhiteSpace(
                actorUserId))
        {
            throw new ArgumentException(
                "Actor user id là bắt buộc.",
                nameof(actorUserId));
        }

        string normalizedActorUserId =
            actorUserId.Trim();

        if (normalizedActorUserId.Length > 100)
        {
            throw new ArgumentException(
                "Actor user id không được vượt quá 100 ký tự.",
                nameof(actorUserId));
        }

        if (string.IsNullOrWhiteSpace(
                actorUsername))
        {
            throw new ArgumentException(
                "Actor username là bắt buộc.",
                nameof(actorUsername));
        }

        string normalizedActorUsername =
            actorUsername.Trim();

        if (normalizedActorUsername.Length > 150)
        {
            throw new ArgumentException(
                "Actor username không được vượt quá 150 ký tự.",
                nameof(actorUsername));
        }

        Status =
            TimesheetPeriodStatus.Closed;

        ClosedAtUtc =
            closedAtUtc;

        ClosedByUserId =
            normalizedActorUserId;

        ClosedByUsername =
            normalizedActorUsername;
    }
}
