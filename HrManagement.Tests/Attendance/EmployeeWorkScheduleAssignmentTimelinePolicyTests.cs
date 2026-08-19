using HrManagement.Domain.Attendance.Schedules;
using HrManagement.Domain.Employees;

namespace HrManagement.Tests.Attendance;

public sealed class EmployeeWorkScheduleAssignmentTimelinePolicyTests
{
    [Fact]
    public void EnsureWithinEmploymentPeriod_WhenValidOpenPeriod_DoesNotThrow()
    {
        Guid employeeId =
            Guid.NewGuid();

        var period =
            new EmploymentPeriod(
                Guid.NewGuid(),
                employeeId,
                new DateOnly(
                    2026,
                    1,
                    1));

        var assignment =
            new EmployeeWorkScheduleAssignment(
                Guid.NewGuid(),
                employeeId,
                period.Id,
                Guid.NewGuid(),
                new DateOnly(
                    2026,
                    8,
                    1));

        Exception? exception =
            Record.Exception(
                () =>
                    EmployeeWorkScheduleAssignmentTimelinePolicy
                        .EnsureWithinEmploymentPeriod(
                            assignment,
                            period));

        Assert.Null(
            exception);
    }

    [Fact]
    public void EnsureWithinEmploymentPeriod_WhenAssignmentStartsBeforePeriod_Throws()
    {
        Guid employeeId =
            Guid.NewGuid();

        var period =
            new EmploymentPeriod(
                Guid.NewGuid(),
                employeeId,
                new DateOnly(
                    2026,
                    2,
                    1));

        var assignment =
            new EmployeeWorkScheduleAssignment(
                Guid.NewGuid(),
                employeeId,
                period.Id,
                Guid.NewGuid(),
                new DateOnly(
                    2026,
                    1,
                    31));

        Assert.Throws<InvalidOperationException>(
            () =>
                EmployeeWorkScheduleAssignmentTimelinePolicy
                    .EnsureWithinEmploymentPeriod(
                        assignment,
                        period));
    }

    [Fact]
    public void EnsureWithinEmploymentPeriod_WhenAssignmentEndsAfterPeriod_Throws()
    {
        Guid employeeId =
            Guid.NewGuid();

        var period =
            new EmploymentPeriod(
                Guid.NewGuid(),
                employeeId,
                new DateOnly(
                    2026,
                    1,
                    1),
                new DateOnly(
                    2026,
                    12,
                    31));

        var assignment =
            new EmployeeWorkScheduleAssignment(
                Guid.NewGuid(),
                employeeId,
                period.Id,
                Guid.NewGuid(),
                new DateOnly(
                    2026,
                    8,
                    1),
                new DateOnly(
                    2027,
                    1,
                    1));

        Assert.Throws<InvalidOperationException>(
            () =>
                EmployeeWorkScheduleAssignmentTimelinePolicy
                    .EnsureWithinEmploymentPeriod(
                        assignment,
                        period));
    }

    [Fact]
    public void EnsureWithinEmploymentPeriod_WhenPeriodClosedAndAssignmentOpen_Throws()
    {
        Guid employeeId =
            Guid.NewGuid();

        var period =
            new EmploymentPeriod(
                Guid.NewGuid(),
                employeeId,
                new DateOnly(
                    2026,
                    1,
                    1),
                new DateOnly(
                    2026,
                    12,
                    31));

        var assignment =
            new EmployeeWorkScheduleAssignment(
                Guid.NewGuid(),
                employeeId,
                period.Id,
                Guid.NewGuid(),
                new DateOnly(
                    2026,
                    8,
                    1));

        Assert.Throws<InvalidOperationException>(
            () =>
                EmployeeWorkScheduleAssignmentTimelinePolicy
                    .EnsureWithinEmploymentPeriod(
                        assignment,
                        period));
    }

    [Fact]
    public void EnsureWithinEmploymentPeriod_WhenEmployeeDoesNotMatch_Throws()
    {
        var period =
            new EmploymentPeriod(
                Guid.NewGuid(),
                Guid.NewGuid(),
                new DateOnly(
                    2026,
                    1,
                    1));

        var assignment =
            new EmployeeWorkScheduleAssignment(
                Guid.NewGuid(),
                Guid.NewGuid(),
                period.Id,
                Guid.NewGuid(),
                new DateOnly(
                    2026,
                    8,
                    1));

        Assert.Throws<InvalidOperationException>(
            () =>
                EmployeeWorkScheduleAssignmentTimelinePolicy
                    .EnsureWithinEmploymentPeriod(
                        assignment,
                        period));
    }

    [Fact]
    public void Overlaps_WhenRangesIntersect_ReturnsTrue()
    {
        Guid employeeId =
            Guid.NewGuid();

        Guid periodId =
            Guid.NewGuid();

        var first =
            new EmployeeWorkScheduleAssignment(
                Guid.NewGuid(),
                employeeId,
                periodId,
                Guid.NewGuid(),
                new DateOnly(
                    2026,
                    8,
                    1),
                new DateOnly(
                    2026,
                    8,
                    20));

        var second =
            new EmployeeWorkScheduleAssignment(
                Guid.NewGuid(),
                employeeId,
                periodId,
                Guid.NewGuid(),
                new DateOnly(
                    2026,
                    8,
                    20),
                new DateOnly(
                    2026,
                    8,
                    31));

        Assert.True(
            EmployeeWorkScheduleAssignmentTimelinePolicy
                .Overlaps(
                    first,
                    second));
    }

    [Fact]
    public void Overlaps_WhenRangesAreAdjacent_ReturnsFalse()
    {
        Guid employeeId =
            Guid.NewGuid();

        Guid periodId =
            Guid.NewGuid();

        var first =
            new EmployeeWorkScheduleAssignment(
                Guid.NewGuid(),
                employeeId,
                periodId,
                Guid.NewGuid(),
                new DateOnly(
                    2026,
                    8,
                    1),
                new DateOnly(
                    2026,
                    8,
                    31));

        var second =
            new EmployeeWorkScheduleAssignment(
                Guid.NewGuid(),
                employeeId,
                periodId,
                Guid.NewGuid(),
                new DateOnly(
                    2026,
                    9,
                    1));

        Assert.False(
            EmployeeWorkScheduleAssignmentTimelinePolicy
                .Overlaps(
                    first,
                    second));
    }

    [Fact]
    public void EnsureNoOverlap_WhenSameTimelineOverlaps_Throws()
    {
        Guid employeeId =
            Guid.NewGuid();

        Guid periodId =
            Guid.NewGuid();

        var existing =
            new EmployeeWorkScheduleAssignment(
                Guid.NewGuid(),
                employeeId,
                periodId,
                Guid.NewGuid(),
                new DateOnly(
                    2026,
                    8,
                    1));

        var candidate =
            new EmployeeWorkScheduleAssignment(
                Guid.NewGuid(),
                employeeId,
                periodId,
                Guid.NewGuid(),
                new DateOnly(
                    2026,
                    9,
                    1));

        Assert.Throws<InvalidOperationException>(
            () =>
                EmployeeWorkScheduleAssignmentTimelinePolicy
                    .EnsureNoOverlap(
                        candidate,
                        [existing]));
    }

    [Fact]
    public void EnsureNoOverlap_WhenEmploymentPeriodDiffers_DoesNotThrow()
    {
        Guid employeeId =
            Guid.NewGuid();

        var existing =
            new EmployeeWorkScheduleAssignment(
                Guid.NewGuid(),
                employeeId,
                Guid.NewGuid(),
                Guid.NewGuid(),
                new DateOnly(
                    2026,
                    8,
                    1));

        var candidate =
            new EmployeeWorkScheduleAssignment(
                Guid.NewGuid(),
                employeeId,
                Guid.NewGuid(),
                Guid.NewGuid(),
                new DateOnly(
                    2026,
                    8,
                    1));

        Exception? exception =
            Record.Exception(
                () =>
                    EmployeeWorkScheduleAssignmentTimelinePolicy
                        .EnsureNoOverlap(
                            candidate,
                            [existing]));

        Assert.Null(
            exception);
    }

    [Fact]
    public void CalculatePreviousEffectiveTo_ReturnsDayBeforeNewAssignment()
    {
        var current =
            new EmployeeWorkScheduleAssignment(
                Guid.NewGuid(),
                Guid.NewGuid(),
                Guid.NewGuid(),
                Guid.NewGuid(),
                new DateOnly(
                    2026,
                    8,
                    1));

        DateOnly result =
            EmployeeWorkScheduleAssignmentTimelinePolicy
                .CalculatePreviousEffectiveTo(
                    current,
                    new DateOnly(
                        2026,
                        9,
                        1));

        Assert.Equal(
            new DateOnly(
                2026,
                8,
                31),
            result);
    }

    [Fact]
    public void CalculatePreviousEffectiveTo_WhenNewDateIsNotLater_Throws()
    {
        var current =
            new EmployeeWorkScheduleAssignment(
                Guid.NewGuid(),
                Guid.NewGuid(),
                Guid.NewGuid(),
                Guid.NewGuid(),
                new DateOnly(
                    2026,
                    8,
                    1));

        Assert.Throws<ArgumentException>(
            () =>
                EmployeeWorkScheduleAssignmentTimelinePolicy
                    .CalculatePreviousEffectiveTo(
                        current,
                        new DateOnly(
                            2026,
                            8,
                            1)));
    }
}
