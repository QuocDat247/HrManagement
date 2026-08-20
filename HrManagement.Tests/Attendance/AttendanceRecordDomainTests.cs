using HrManagement.Domain.Attendance.Records;

namespace HrManagement.Tests.Attendance;

public sealed class AttendanceRecordDomainTests
{
    [Fact]
    public void WorkingDay_CreatesExpectedScheduleSnapshot()
    {
        var record =
            CreateWorkingRecord(
                new TimeOnly(
                    8,
                    0),
                new TimeOnly(
                    17,
                    0),
                60);

        Assert.True(
            record.IsWorkingDay);

        Assert.False(
            record.IsOvernight);

        Assert.Equal(
            480,
            record.ExpectedPlannedMinutes);

        Assert.Equal(
            "SE Asia Standard Time",
            record.TimeZoneId);
    }

    [Fact]
    public void OvernightWorkingDay_CalculatesExpectedPlannedMinutes()
    {
        var record =
            CreateWorkingRecord(
                new TimeOnly(
                    22,
                    0),
                new TimeOnly(
                    6,
                    0),
                60);

        Assert.True(
            record.IsOvernight);

        Assert.Equal(
            420,
            record.ExpectedPlannedMinutes);
    }

    [Fact]
    public void NonWorkingDay_CreatesZeroExpectation()
    {
        var record =
            new AttendanceRecord(
                Guid.NewGuid(),
                Guid.NewGuid(),
                Guid.NewGuid(),
                Guid.NewGuid(),
                Guid.NewGuid(),
                new DateOnly(
                    2026,
                    8,
                    23),
                "SE Asia Standard Time",
                isWorkingDay: false);

        Assert.False(
            record.IsWorkingDay);

        Assert.False(
            record.IsOvernight);

        Assert.Null(
            record.ExpectedStartTime);

        Assert.Null(
            record.ExpectedEndTime);

        Assert.Equal(
            0,
            record.ExpectedBreakMinutes);

        Assert.Equal(
            0,
            record.ExpectedPlannedMinutes);
    }

    [Fact]
    public void Constructor_WhenWorkDateIsDefault_Throws()
    {
        Assert.Throws<ArgumentException>(
            () =>
                new AttendanceRecord(
                    Guid.NewGuid(),
                    Guid.NewGuid(),
                    Guid.NewGuid(),
                    Guid.NewGuid(),
                    Guid.NewGuid(),
                    default,
                    "SE Asia Standard Time",
                    false));
    }

    [Fact]
    public void Constructor_WhenTimeZoneIsBlank_Throws()
    {
        Assert.Throws<ArgumentException>(
            () =>
                new AttendanceRecord(
                    Guid.NewGuid(),
                    Guid.NewGuid(),
                    Guid.NewGuid(),
                    Guid.NewGuid(),
                    Guid.NewGuid(),
                    new DateOnly(
                        2026,
                        8,
                        20),
                    " ",
                    false));
    }

    [Fact]
    public void WorkingDay_WhenExpectedTimesAreMissing_Throws()
    {
        Assert.Throws<ArgumentException>(
            () =>
                new AttendanceRecord(
                    Guid.NewGuid(),
                    Guid.NewGuid(),
                    Guid.NewGuid(),
                    Guid.NewGuid(),
                    Guid.NewGuid(),
                    new DateOnly(
                        2026,
                        8,
                        20),
                    "SE Asia Standard Time",
                    true));
    }

    [Fact]
    public void NonWorkingDay_WhenExpectedTimesAreProvided_Throws()
    {
        Assert.Throws<ArgumentException>(
            () =>
                new AttendanceRecord(
                    Guid.NewGuid(),
                    Guid.NewGuid(),
                    Guid.NewGuid(),
                    Guid.NewGuid(),
                    Guid.NewGuid(),
                    new DateOnly(
                        2026,
                        8,
                        23),
                    "SE Asia Standard Time",
                    false,
                    new TimeOnly(
                        8,
                        0),
                    new TimeOnly(
                        17,
                        0)));
    }

    [Fact]
    public void WorkingDay_WhenBreakConsumesWholeShift_Throws()
    {
        Assert.Throws<ArgumentException>(
            () =>
                CreateWorkingRecord(
                    new TimeOnly(
                        8,
                        0),
                    new TimeOnly(
                        12,
                        0),
                    240));
    }

    private static AttendanceRecord CreateWorkingRecord(
        TimeOnly startTime,
        TimeOnly endTime,
        int breakMinutes)
    {
        return new AttendanceRecord(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            new DateOnly(
                2026,
                8,
                20),
            " SE Asia Standard Time ",
            true,
            startTime,
            endTime,
            breakMinutes);
    }
}
