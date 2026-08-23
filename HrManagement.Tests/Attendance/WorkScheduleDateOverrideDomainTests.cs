using HrManagement.Domain.Attendance.Schedules;

namespace HrManagement.Tests.Attendance;

public sealed class WorkScheduleDateOverrideDomainTests
{
    [Fact]
    public void Constructor_WithWorkingDay_CalculatesPlannedMinutes()
    {
        var item =
            new WorkScheduleDateOverride(
                Guid.NewGuid(),
                Guid.NewGuid(),
                new DateOnly(
                    2026,
                    9,
                    5),
                true,
                new TimeOnly(
                    8,
                    0),
                new TimeOnly(
                    17,
                    0),
                60,
                "  Làm bù  ");

        Assert.True(
            item.IsWorkingDay);

        Assert.Equal(
            new TimeOnly(
                8,
                0),
            item.StartTime);

        Assert.Equal(
            new TimeOnly(
                17,
                0),
            item.EndTime);

        Assert.Equal(
            60,
            item.BreakMinutes);

        Assert.Equal(
            480,
            item.PlannedMinutes);

        Assert.False(
            item.IsOvernight);

        Assert.Equal(
            "Làm bù",
            item.Note);
    }

    [Fact]
    public void Constructor_WithNonWorkingDay_CreatesZeroExpectation()
    {
        var item =
            new WorkScheduleDateOverride(
                Guid.NewGuid(),
                Guid.NewGuid(),
                new DateOnly(
                    2026,
                    9,
                    2),
                false,
                note:
                    "  Nghỉ điều chỉnh  ");

        Assert.False(
            item.IsWorkingDay);

        Assert.Null(
            item.StartTime);

        Assert.Null(
            item.EndTime);

        Assert.Equal(
            0,
            item.BreakMinutes);

        Assert.Equal(
            0,
            item.PlannedMinutes);

        Assert.False(
            item.IsOvernight);

        Assert.Equal(
            "Nghỉ điều chỉnh",
            item.Note);
    }

    [Fact]
    public void Constructor_WithOvernightShift_CalculatesAcrossMidnight()
    {
        var item =
            new WorkScheduleDateOverride(
                Guid.NewGuid(),
                Guid.NewGuid(),
                new DateOnly(
                    2026,
                    9,
                    2),
                true,
                new TimeOnly(
                    22,
                    0),
                new TimeOnly(
                    6,
                    0),
                30,
                "Trực ngày lễ");

        Assert.True(
            item.IsOvernight);

        Assert.Equal(
            450,
            item.PlannedMinutes);
    }

    [Fact]
    public void Constructor_WithEmptyId_Throws()
    {
        Assert.Throws<ArgumentException>(
            () =>
                new WorkScheduleDateOverride(
                    Guid.Empty,
                    Guid.NewGuid(),
                    new DateOnly(
                        2026,
                        9,
                        5),
                    false));
    }

    [Fact]
    public void Constructor_WithEmptyWorkScheduleId_Throws()
    {
        Assert.Throws<ArgumentException>(
            () =>
                new WorkScheduleDateOverride(
                    Guid.NewGuid(),
                    Guid.Empty,
                    new DateOnly(
                        2026,
                        9,
                        5),
                    false));
    }

    [Fact]
    public void Constructor_WithDefaultDate_Throws()
    {
        Assert.Throws<ArgumentException>(
            () =>
                new WorkScheduleDateOverride(
                    Guid.NewGuid(),
                    Guid.NewGuid(),
                    default,
                    false));
    }

    [Fact]
    public void Constructor_WithNonWorkingDayAndTimes_Throws()
    {
        Assert.Throws<ArgumentException>(
            () =>
                new WorkScheduleDateOverride(
                    Guid.NewGuid(),
                    Guid.NewGuid(),
                    new DateOnly(
                        2026,
                        9,
                        5),
                    false,
                    new TimeOnly(
                        8,
                        0),
                    new TimeOnly(
                        17,
                        0)));
    }

    [Fact]
    public void Constructor_WithWorkingDayWithoutTimes_Throws()
    {
        Assert.Throws<ArgumentException>(
            () =>
                new WorkScheduleDateOverride(
                    Guid.NewGuid(),
                    Guid.NewGuid(),
                    new DateOnly(
                        2026,
                        9,
                        5),
                    true));
    }

    [Fact]
    public void Constructor_WithEqualStartAndEnd_Throws()
    {
        Assert.Throws<ArgumentException>(
            () =>
                new WorkScheduleDateOverride(
                    Guid.NewGuid(),
                    Guid.NewGuid(),
                    new DateOnly(
                        2026,
                        9,
                        5),
                    true,
                    new TimeOnly(
                        8,
                        0),
                    new TimeOnly(
                        8,
                        0)));
    }

    [Fact]
    public void Constructor_WithNegativeBreak_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () =>
                new WorkScheduleDateOverride(
                    Guid.NewGuid(),
                    Guid.NewGuid(),
                    new DateOnly(
                        2026,
                        9,
                        5),
                    true,
                    new TimeOnly(
                        8,
                        0),
                    new TimeOnly(
                        17,
                        0),
                    -1));
    }

    [Fact]
    public void Constructor_WithBreakEqualToShift_Throws()
    {
        Assert.Throws<ArgumentException>(
            () =>
                new WorkScheduleDateOverride(
                    Guid.NewGuid(),
                    Guid.NewGuid(),
                    new DateOnly(
                        2026,
                        9,
                        5),
                    true,
                    new TimeOnly(
                        8,
                        0),
                    new TimeOnly(
                        9,
                        0),
                    60));
    }

    [Fact]
    public void Constructor_WithSecondPrecision_Throws()
    {
        Assert.Throws<ArgumentException>(
            () =>
                new WorkScheduleDateOverride(
                    Guid.NewGuid(),
                    Guid.NewGuid(),
                    new DateOnly(
                        2026,
                        9,
                        5),
                    true,
                    new TimeOnly(
                        8,
                        0,
                        1),
                    new TimeOnly(
                        17,
                        0)));
    }
}
