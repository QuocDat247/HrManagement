using HrManagement.Domain.Attendance.Timesheets;

namespace HrManagement.Tests.Attendance;

public sealed class TimesheetPeriodDomainTests
{
    [Fact]
    public void Constructor_CreatesOpenCalendarMonth()
    {
        var period =
            new TimesheetPeriod(
                Guid.NewGuid(),
                2026,
                8);

        Assert.Equal(
            TimesheetPeriodStatus.Open,
            period.Status);

        Assert.Equal(
            new DateOnly(
                2026,
                8,
                1),
            period.StartDate);

        Assert.Equal(
            new DateOnly(
                2026,
                8,
                31),
            period.EndDate);

        Assert.False(
            period.IsClosed);

        Assert.Null(
            period.ClosedAtUtc);

        Assert.Null(
            period.ClosedByUserId);

        Assert.Null(
            period.ClosedByUsername);
    }

    [Fact]
    public void Close_RecordsActorAndUtcTimestamp()
    {
        var period =
            new TimesheetPeriod(
                Guid.NewGuid(),
                2026,
                8);

        DateTime closedAtUtc =
            new(
                2026,
                8,
                31,
                12,
                0,
                0,
                DateTimeKind.Utc);

        period.Close(
            closedAtUtc,
            "user-1",
            "admin");

        Assert.True(
            period.IsClosed);

        Assert.Equal(
            TimesheetPeriodStatus.Closed,
            period.Status);

        Assert.Equal(
            closedAtUtc,
            period.ClosedAtUtc);

        Assert.Equal(
            "user-1",
            period.ClosedByUserId);

        Assert.Equal(
            "admin",
            period.ClosedByUsername);
    }

    [Fact]
    public void Close_WhenAlreadyClosed_Throws()
    {
        var period =
            new TimesheetPeriod(
                Guid.NewGuid(),
                2026,
                8);

        DateTime closedAtUtc =
            new(
                2026,
                8,
                31,
                12,
                0,
                0,
                DateTimeKind.Utc);

        period.Close(
            closedAtUtc,
            "user-1",
            "admin");

        InvalidOperationException exception =
            Assert.Throws<InvalidOperationException>(
                () =>
                    period.Close(
                        closedAtUtc.AddMinutes(
                            1),
                        "user-1",
                        "admin"));

        Assert.Equal(
            "Kỳ công đã được đóng.",
            exception.Message);
    }

    [Fact]
    public void Close_WhenTimestampIsNotUtc_Throws()
    {
        var period =
            new TimesheetPeriod(
                Guid.NewGuid(),
                2026,
                8);

        Assert.Throws<ArgumentException>(
            () =>
                period.Close(
                    new DateTime(
                        2026,
                        8,
                        31,
                        12,
                        0,
                        0,
                        DateTimeKind.Local),
                    "user-1",
                    "admin"));
    }
}
