namespace HrManagement.Domain.Payroll.Periods;

public sealed class PayrollPeriod
{
    public Guid Id
    {
        get;
    }

    public Guid TimesheetPeriodId
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

    public PayrollPeriodStatus Status
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
        PayrollPeriodStatus.Closed;

    public PayrollPeriod(
        Guid id,
        Guid timesheetPeriodId,
        int year,
        int month)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException(
                "Mã kỳ lương không hợp lệ.",
                nameof(id));
        }

        if (timesheetPeriodId == Guid.Empty)
        {
            throw new ArgumentException(
                "Mã kỳ công nguồn không hợp lệ.",
                nameof(timesheetPeriodId));
        }

        if (year < 2000
            || year > 9999)
        {
            throw new ArgumentOutOfRangeException(
                nameof(year),
                "Năm kỳ lương không hợp lệ.");
        }

        if (month < 1
            || month > 12)
        {
            throw new ArgumentOutOfRangeException(
                nameof(month),
                "Tháng kỳ lương phải từ 1 đến 12.");
        }

        Id =
            id;

        TimesheetPeriodId =
            timesheetPeriodId;

        Year =
            year;

        Month =
            month;

        Status =
            PayrollPeriodStatus.Open;
    }

    public void Close(
        DateTime closedAtUtc,
        string actorUserId,
        string actorUsername)
    {
        if (IsClosed)
        {
            throw new InvalidOperationException(
                "Kỳ lương đã được đóng.");
        }

        if (closedAtUtc == default
            || closedAtUtc.Kind !=
                DateTimeKind.Utc)
        {
            throw new ArgumentException(
                "Thời điểm đóng kỳ lương phải sử dụng UTC.",
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
            PayrollPeriodStatus.Closed;

        ClosedAtUtc =
            closedAtUtc;

        ClosedByUserId =
            normalizedActorUserId;

        ClosedByUsername =
            normalizedActorUsername;
    }
}
