using HrManagement.Domain.Attendance.Schedules;

namespace HrManagement.Tests.Attendance;

public sealed class WorkScheduleDomainTests
{
    [Fact]
    public void WorkSchedule_NormalizesTextValues()
    {
        Guid id =
            Guid.NewGuid();

        var schedule =
            new WorkSchedule(
                id,
                " office ",
                " Văn phòng ",
                " SE Asia Standard Time ");

        Assert.Equal(
            id,
            schedule.Id);

        Assert.Equal(
            "OFFICE",
            schedule.Code);

        Assert.Equal(
            "Văn phòng",
            schedule.Name);

        Assert.Equal(
            "SE Asia Standard Time",
            schedule.TimeZoneId);

        Assert.True(
            schedule.IsActive);
    }

    [Fact]
    public void WorkSchedule_WhenIdIsEmpty_Throws()
    {
        Assert.Throws<ArgumentException>(
            () =>
                new WorkSchedule(
                    Guid.Empty,
                    "OFFICE",
                    "Văn phòng",
                    "SE Asia Standard Time"));
    }

    [Fact]
    public void WorkSchedule_WhenTimeZoneIsBlank_Throws()
    {
        Assert.Throws<ArgumentException>(
            () =>
                new WorkSchedule(
                    Guid.NewGuid(),
                    "OFFICE",
                    "Văn phòng",
                    " "));
    }

    [Fact]
    public void WorkingDay_CalculatesSameDayPlannedMinutes()
    {
        var day =
            new WorkScheduleDay(
                Guid.NewGuid(),
                Guid.NewGuid(),
                DayOfWeek.Monday,
                true,
                new TimeOnly(
                    8,
                    0),
                new TimeOnly(
                    17,
                    0),
                breakMinutes:
                    60);

        Assert.True(
            day.IsWorkingDay);

        Assert.False(
            day.IsOvernight);

        Assert.Equal(
            480,
            day.PlannedMinutes);
    }

    [Fact]
    public void WorkingDay_CalculatesOvernightPlannedMinutes()
    {
        var day =
            new WorkScheduleDay(
                Guid.NewGuid(),
                Guid.NewGuid(),
                DayOfWeek.Monday,
                true,
                new TimeOnly(
                    22,
                    0),
                new TimeOnly(
                    6,
                    0),
                breakMinutes:
                    60);

        Assert.True(
            day.IsOvernight);

        Assert.Equal(
            420,
            day.PlannedMinutes);
    }

    [Fact]
    public void WorkingDay_WhenStartAndEndAreEqual_Throws()
    {
        Assert.Throws<ArgumentException>(
            () =>
                new WorkScheduleDay(
                    Guid.NewGuid(),
                    Guid.NewGuid(),
                    DayOfWeek.Monday,
                    true,
                    new TimeOnly(
                        8,
                        0),
                    new TimeOnly(
                        8,
                        0)));
    }

    [Fact]
    public void WorkingDay_WhenBreakConsumesWholeShift_Throws()
    {
        Assert.Throws<ArgumentException>(
            () =>
                new WorkScheduleDay(
                    Guid.NewGuid(),
                    Guid.NewGuid(),
                    DayOfWeek.Monday,
                    true,
                    new TimeOnly(
                        8,
                        0),
                    new TimeOnly(
                        12,
                        0),
                    breakMinutes:
                        240));
    }

    [Fact]
    public void NonWorkingDay_WithoutTimes_HasZeroPlannedMinutes()
    {
        var day =
            new WorkScheduleDay(
                Guid.NewGuid(),
                Guid.NewGuid(),
                DayOfWeek.Sunday,
                false);

        Assert.False(
            day.IsWorkingDay);

        Assert.Null(
            day.StartTime);

        Assert.Null(
            day.EndTime);

        Assert.Equal(
            0,
            day.BreakMinutes);

        Assert.Equal(
            0,
            day.PlannedMinutes);
    }

    [Fact]
    public void NonWorkingDay_WhenTimeIsProvided_Throws()
    {
        Assert.Throws<ArgumentException>(
            () =>
                new WorkScheduleDay(
                    Guid.NewGuid(),
                    Guid.NewGuid(),
                    DayOfWeek.Sunday,
                    false,
                    new TimeOnly(
                        8,
                        0),
                    new TimeOnly(
                        17,
                        0)));
    }

    [Fact]
    public void WorkScheduleDay_WhenDayOfWeekIsUndefined_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () =>
                new WorkScheduleDay(
                    Guid.NewGuid(),
                    Guid.NewGuid(),
                    (DayOfWeek)99,
                    false));
    }

    [Fact]
    public void WorkingDay_WhenTimeContainsSeconds_Throws()
    {
        Assert.Throws<ArgumentException>(
            () =>
                new WorkScheduleDay(
                    Guid.NewGuid(),
                    Guid.NewGuid(),
                    DayOfWeek.Monday,
                    true,
                    new TimeOnly(
                        8,
                        0,
                        30),
                    new TimeOnly(
                        17,
                        0)));
    }
}
