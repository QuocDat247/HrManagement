using HrManagement.Domain.Employees;

namespace HrManagement.Tests.Employees;

public sealed class EmploymentHistoryTests
{
    [Fact]
    public void Constructor_WithNonOverlappingPeriods_CreatesOrderedHistory()
    {
        Guid employeeId =
            Guid.NewGuid();

        var newerPeriod =
            new EmploymentPeriod(
                Guid.NewGuid(),
                employeeId,
                new DateOnly(2026, 2, 10));

        var olderPeriod =
            new EmploymentPeriod(
                Guid.NewGuid(),
                employeeId,
                new DateOnly(2023, 3, 1),
                new DateOnly(2025, 6, 15));

        var history =
            new EmploymentHistory(
                employeeId,
                [
                    newerPeriod,
                    olderPeriod
                ]);

        Assert.Equal(
            2,
            history.Periods.Count);

        Assert.Same(
            olderPeriod,
            history.Periods[0]);

        Assert.Same(
            newerPeriod,
            history.Periods[1]);

        Assert.Same(
            newerPeriod,
            history.CurrentPeriod);
    }

    [Fact]
    public void Constructor_WithOverlappingPeriods_Throws()
    {
        Guid employeeId =
            Guid.NewGuid();

        var firstPeriod =
            new EmploymentPeriod(
                Guid.NewGuid(),
                employeeId,
                new DateOnly(2024, 1, 1),
                new DateOnly(2025, 6, 30));

        var secondPeriod =
            new EmploymentPeriod(
                Guid.NewGuid(),
                employeeId,
                new DateOnly(2025, 6, 1));

        Assert.Throws<ArgumentException>(
            () => new EmploymentHistory(
                employeeId,
                [
                    firstPeriod,
                    secondPeriod
                ]));
    }

    [Fact]
    public void Constructor_WithTwoOpenPeriods_Throws()
    {
        Guid employeeId =
            Guid.NewGuid();

        var firstPeriod =
            new EmploymentPeriod(
                Guid.NewGuid(),
                employeeId,
                new DateOnly(2024, 1, 1));

        var secondPeriod =
            new EmploymentPeriod(
                Guid.NewGuid(),
                employeeId,
                new DateOnly(2026, 1, 1));

        Assert.Throws<ArgumentException>(
            () => new EmploymentHistory(
                employeeId,
                [
                    firstPeriod,
                    secondPeriod
                ]));
    }

    [Fact]
    public void Constructor_WhenPeriodBelongsToAnotherEmployee_Throws()
    {
        Guid employeeId =
            Guid.NewGuid();

        var period =
            new EmploymentPeriod(
                Guid.NewGuid(),
                Guid.NewGuid(),
                new DateOnly(2026, 1, 1));

        Assert.Throws<ArgumentException>(
            () => new EmploymentHistory(
                employeeId,
                [period]));
    }

    [Fact]
    public void Constructor_WithAdjacentPeriodsOnDifferentDays_IsValid()
    {
        Guid employeeId =
            Guid.NewGuid();

        var firstPeriod =
            new EmploymentPeriod(
                Guid.NewGuid(),
                employeeId,
                new DateOnly(2024, 1, 1),
                new DateOnly(2025, 6, 15));

        var secondPeriod =
            new EmploymentPeriod(
                Guid.NewGuid(),
                employeeId,
                new DateOnly(2025, 6, 16));

        var history =
            new EmploymentHistory(
                employeeId,
                [
                    firstPeriod,
                    secondPeriod
                ]);

        Assert.Equal(
            2,
            history.Periods.Count);

        Assert.Same(
            secondPeriod,
            history.CurrentPeriod);
    }

    [Fact]
    public void CloseCurrentPeriod_WhenOpenPeriodExists_ClosesCurrentPeriod()
    {
        Guid employeeId =
            Guid.NewGuid();

        var closedPeriod =
            new EmploymentPeriod(
                Guid.NewGuid(),
                employeeId,
                new DateOnly(2022, 1, 1),
                new DateOnly(2024, 12, 31));

        var currentPeriod =
            new EmploymentPeriod(
                Guid.NewGuid(),
                employeeId,
                new DateOnly(2025, 2, 1));

        var history =
            new EmploymentHistory(
                employeeId,
                [
                    closedPeriod,
                currentPeriod
                ]);

        DateOnly terminationDate =
            new(2026, 8, 12);

        EmploymentPeriod result =
            history.CloseCurrentPeriod(
                terminationDate);

        Assert.Same(
            currentPeriod,
            result);

        Assert.Equal(
            terminationDate,
            currentPeriod.EndDate);

        Assert.False(
            currentPeriod.IsOpen);

        Assert.Null(
            history.CurrentPeriod);
    }

    [Fact]
    public void ReopenLatestPeriod_WhenLatestPeriodMatches_ReopensPeriod()
    {
        Guid employeeId =
            Guid.NewGuid();

        DateOnly terminationDate =
            new(2026, 6, 15);

        var period =
            new EmploymentPeriod(
                Guid.NewGuid(),
                employeeId,
                new DateOnly(2024, 1, 1),
                terminationDate);

        var history =
            new EmploymentHistory(
                employeeId,
                [period]);

        EmploymentPeriod reopenedPeriod =
            history.ReopenLatestPeriod(
                terminationDate);

        Assert.Same(
            period,
            reopenedPeriod);

        Assert.Null(
            reopenedPeriod.EndDate);

        Assert.True(
            reopenedPeriod.IsOpen);

        Assert.Same(
            reopenedPeriod,
            history.CurrentPeriod);
    }

    [Fact]
    public void ReopenLatestPeriod_WhenHistoryIsEmpty_Throws()
    {
        Guid employeeId =
            Guid.NewGuid();

        var history =
            new EmploymentHistory(
                employeeId,
                []);

        Assert.Throws<InvalidOperationException>(
            () => history.ReopenLatestPeriod(
                new DateOnly(2026, 6, 15)));
    }

    [Fact]
    public void ReopenLatestPeriod_WhenOpenPeriodAlreadyExists_Throws()
    {
        Guid employeeId =
            Guid.NewGuid();

        var period =
            new EmploymentPeriod(
                Guid.NewGuid(),
                employeeId,
                new DateOnly(2025, 1, 1));

        var history =
            new EmploymentHistory(
                employeeId,
                [period]);

        Assert.Throws<InvalidOperationException>(
            () => history.ReopenLatestPeriod(
                new DateOnly(2026, 6, 15)));

        Assert.True(
            period.IsOpen);
    }

    [Fact]
    public void ReopenLatestPeriod_WhenTerminationDateDoesNotMatch_Throws()
    {
        Guid employeeId =
            Guid.NewGuid();

        var period =
            new EmploymentPeriod(
                Guid.NewGuid(),
                employeeId,
                new DateOnly(2024, 1, 1),
                new DateOnly(2026, 6, 15));

        var history =
            new EmploymentHistory(
                employeeId,
                [period]);

        Assert.Throws<InvalidOperationException>(
            () => history.ReopenLatestPeriod(
                new DateOnly(2026, 6, 20)));

        Assert.Equal(
            new DateOnly(2026, 6, 15),
            period.EndDate);

        Assert.False(
            period.IsOpen);
    }

    [Fact]
    public void StartNewPeriod_WhenPreviousPeriodIsClosed_AddsNewOpenPeriod()
    {
        Guid employeeId =
            Guid.NewGuid();

        var previousPeriod =
            new EmploymentPeriod(
                Guid.NewGuid(),
                employeeId,
                new DateOnly(2022, 1, 1),
                new DateOnly(2026, 6, 15));

        var history =
            new EmploymentHistory(
                employeeId,
                [previousPeriod]);

        Guid newPeriodId =
            Guid.NewGuid();

        DateOnly rehireDate =
            new(2026, 9, 1);

        EmploymentPeriod newPeriod =
            history.StartNewPeriod(
                newPeriodId,
                rehireDate);

        Assert.Equal(
            2,
            history.Periods.Count);

        Assert.Same(
            previousPeriod,
            history.Periods[0]);

        Assert.Equal(
            newPeriodId,
            newPeriod.Id);

        Assert.Equal(
            rehireDate,
            newPeriod.StartDate);

        Assert.Null(
            newPeriod.EndDate);

        Assert.True(
            newPeriod.IsOpen);

        Assert.Same(
            newPeriod,
            history.CurrentPeriod);

        Assert.Equal(
            new DateOnly(2026, 6, 15),
            previousPeriod.EndDate);
    }

    [Fact]
    public void StartNewPeriod_WhenStartDateIsNotAfterPreviousEndDate_Throws()
    {
        Guid employeeId =
            Guid.NewGuid();

        var previousPeriod =
            new EmploymentPeriod(
                Guid.NewGuid(),
                employeeId,
                new DateOnly(2022, 1, 1),
                new DateOnly(2026, 6, 15));

        var history =
            new EmploymentHistory(
                employeeId,
                [previousPeriod]);

        Assert.Throws<ArgumentException>(
            () => history.StartNewPeriod(
                Guid.NewGuid(),
                new DateOnly(2026, 6, 15)));

        Assert.Single(
            history.Periods);

        Assert.Null(
            history.CurrentPeriod);
    }

    [Fact]
    public void StartNewPeriod_WhenOpenPeriodAlreadyExists_Throws()
    {
        Guid employeeId =
            Guid.NewGuid();

        var currentPeriod =
            new EmploymentPeriod(
                Guid.NewGuid(),
                employeeId,
                new DateOnly(2025, 1, 1));

        var history =
            new EmploymentHistory(
                employeeId,
                [currentPeriod]);

        Assert.Throws<InvalidOperationException>(
            () => history.StartNewPeriod(
                Guid.NewGuid(),
                new DateOnly(2026, 9, 1)));

        Assert.Single(
            history.Periods);

        Assert.Same(
            currentPeriod,
            history.CurrentPeriod);
    }

    [Fact]
    public void StartNewPeriod_WhenHistoryIsEmpty_Throws()
    {
        Guid employeeId =
            Guid.NewGuid();

        var history =
            new EmploymentHistory(
                employeeId,
                []);

        Assert.Throws<InvalidOperationException>(
            () => history.StartNewPeriod(
                Guid.NewGuid(),
                new DateOnly(2026, 9, 1)));

        Assert.Empty(
            history.Periods);
    }
}
