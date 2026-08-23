using HrManagement.Domain.Attendance.Calendars;

namespace HrManagement.Tests.Attendance;

public sealed class HolidayCalendarDayDomainTests
{
    [Fact]
    public void Constructor_WithValidValues_CreatesHoliday()
    {
        Guid id =
            Guid.NewGuid();

        DateOnly date =
            new(
                2026,
                9,
                2);

        var holiday =
            new HolidayCalendarDay(
                id,
                date,
                "  Quốc khánh  ");

        Assert.Equal(
            id,
            holiday.Id);

        Assert.Equal(
            date,
            holiday.Date);

        Assert.Equal(
            "Quốc khánh",
            holiday.Name);

        Assert.True(
            holiday.IsActive);
    }

    [Fact]
    public void Constructor_WithEmptyId_Throws()
    {
        Assert.Throws<ArgumentException>(
            () =>
                new HolidayCalendarDay(
                    Guid.Empty,
                    new DateOnly(
                        2026,
                        9,
                        2),
                    "Quốc khánh"));
    }

    [Fact]
    public void Constructor_WithDefaultDate_Throws()
    {
        Assert.Throws<ArgumentException>(
            () =>
                new HolidayCalendarDay(
                    Guid.NewGuid(),
                    default,
                    "Quốc khánh"));
    }

    [Fact]
    public void Constructor_WithBlankName_Throws()
    {
        Assert.Throws<ArgumentException>(
            () =>
                new HolidayCalendarDay(
                    Guid.NewGuid(),
                    new DateOnly(
                        2026,
                        9,
                        2),
                    "   "));
    }

    [Fact]
    public void Rename_WithValidName_TrimsAndUpdatesName()
    {
        var holiday =
            new HolidayCalendarDay(
                Guid.NewGuid(),
                new DateOnly(
                    2026,
                    9,
                    2),
                "Quốc khánh");

        holiday.Rename(
            "  Quốc khánh Việt Nam  ");

        Assert.Equal(
            "Quốc khánh Việt Nam",
            holiday.Name);
    }

    [Fact]
    public void Rename_WithBlankName_ThrowsAndKeepsExistingName()
    {
        var holiday =
            new HolidayCalendarDay(
                Guid.NewGuid(),
                new DateOnly(
                    2026,
                    9,
                    2),
                "Quốc khánh");

        Assert.Throws<ArgumentException>(
            () =>
                holiday.Rename(
                    "   "));

        Assert.Equal(
            "Quốc khánh",
            holiday.Name);
    }

    [Fact]
    public void DeactivateAndReactivate_UpdatesActiveState()
    {
        var holiday =
            new HolidayCalendarDay(
                Guid.NewGuid(),
                new DateOnly(
                    2026,
                    9,
                    2),
                "Quốc khánh");

        holiday.Deactivate();

        Assert.False(
            holiday.IsActive);

        holiday.Reactivate();

        Assert.True(
            holiday.IsActive);
    }
}
