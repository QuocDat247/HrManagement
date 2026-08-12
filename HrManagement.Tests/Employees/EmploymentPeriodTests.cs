using HrManagement.Domain.Employees;

namespace HrManagement.Tests.Employees;

public sealed class EmploymentPeriodTests
{
    [Fact]
    public void Constructor_WithOpenPeriod_CreatesValidPeriod()
    {
        Guid periodId =
            Guid.NewGuid();

        Guid employeeId =
            Guid.NewGuid();

        DateOnly startDate =
            new(2026, 2, 10);

        var period =
            new EmploymentPeriod(
                periodId,
                employeeId,
                startDate);

        Assert.Equal(
            periodId,
            period.Id);

        Assert.Equal(
            employeeId,
            period.EmployeeId);

        Assert.Equal(
            startDate,
            period.StartDate);

        Assert.Null(
            period.EndDate);

        Assert.True(
            period.IsOpen);
    }

    [Fact]
    public void Constructor_WithClosedPeriod_CreatesValidPeriod()
    {
        DateOnly startDate =
            new(2023, 3, 1);

        DateOnly endDate =
            new(2025, 6, 15);

        var period =
            new EmploymentPeriod(
                Guid.NewGuid(),
                Guid.NewGuid(),
                startDate,
                endDate);

        Assert.Equal(
            endDate,
            period.EndDate);

        Assert.False(
            period.IsOpen);
    }

    [Fact]
    public void Constructor_WhenStartDateIsInvalid_Throws()
    {
        Assert.Throws<ArgumentException>(
            () => new EmploymentPeriod(
                Guid.NewGuid(),
                Guid.NewGuid(),
                default));
    }

    [Fact]
    public void Constructor_WhenEndDateIsBeforeStartDate_Throws()
    {
        DateOnly startDate =
            new(2026, 5, 10);

        DateOnly endDate =
            new(2026, 5, 9);

        Assert.Throws<ArgumentException>(
            () => new EmploymentPeriod(
                Guid.NewGuid(),
                Guid.NewGuid(),
                startDate,
                endDate));
    }

    [Fact]
    public void Close_WhenPeriodIsOpen_ClosesPeriod()
    {
        var period =
            new EmploymentPeriod(
                Guid.NewGuid(),
                Guid.NewGuid(),
                new DateOnly(2026, 8, 1));

        DateOnly endDate =
            new(2026, 8, 12);

        period.Close(endDate);

        Assert.Equal(
            endDate,
            period.EndDate);

        Assert.False(
            period.IsOpen);
    }

    [Fact]
    public void Close_WhenEndDateIsBeforeStartDate_Throws()
    {
        var period =
            new EmploymentPeriod(
                Guid.NewGuid(),
                Guid.NewGuid(),
                new DateOnly(2026, 8, 10));

        Assert.Throws<ArgumentException>(
            () => period.Close(
                new DateOnly(2026, 8, 9)));

        Assert.True(period.IsOpen);
    }

    [Fact]
    public void Close_WhenPeriodIsAlreadyClosed_Throws()
    {
        var period =
            new EmploymentPeriod(
                Guid.NewGuid(),
                Guid.NewGuid(),
                new DateOnly(2026, 1, 1),
                new DateOnly(2026, 6, 30));

        Assert.Throws<InvalidOperationException>(
            () => period.Close(
                new DateOnly(2026, 7, 1)));

        Assert.Equal(
            new DateOnly(2026, 6, 30),
            period.EndDate);
    }
}
