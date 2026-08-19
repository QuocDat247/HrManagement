using HrManagement.Domain.Attendance.Schedules;

namespace HrManagement.Tests.Attendance;

public sealed class EmployeeWorkScheduleAssignmentTests
{
    [Fact]
    public void Constructor_WithOpenAssignment_CreatesValidAssignment()
    {
        Guid id =
            Guid.NewGuid();

        Guid employeeId =
            Guid.NewGuid();

        Guid employmentPeriodId =
            Guid.NewGuid();

        Guid workScheduleId =
            Guid.NewGuid();

        DateOnly effectiveFrom =
            new(
                2026,
                8,
                1);

        var assignment =
            new EmployeeWorkScheduleAssignment(
                id,
                employeeId,
                employmentPeriodId,
                workScheduleId,
                effectiveFrom);

        Assert.Equal(
            id,
            assignment.Id);

        Assert.Equal(
            employeeId,
            assignment.EmployeeId);

        Assert.Equal(
            employmentPeriodId,
            assignment.EmploymentPeriodId);

        Assert.Equal(
            workScheduleId,
            assignment.WorkScheduleId);

        Assert.Equal(
            effectiveFrom,
            assignment.EffectiveFrom);

        Assert.Null(
            assignment.EffectiveTo);

        Assert.True(
            assignment.IsOpen);
    }

    [Fact]
    public void Constructor_WithClosedAssignment_CreatesValidAssignment()
    {
        var assignment =
            new EmployeeWorkScheduleAssignment(
                Guid.NewGuid(),
                Guid.NewGuid(),
                Guid.NewGuid(),
                Guid.NewGuid(),
                new DateOnly(
                    2026,
                    8,
                    1),
                new DateOnly(
                    2026,
                    8,
                    31));

        Assert.False(
            assignment.IsOpen);

        Assert.Equal(
            new DateOnly(
                2026,
                8,
                31),
            assignment.EffectiveTo);
    }

    [Fact]
    public void Constructor_WhenIdIsEmpty_Throws()
    {
        Assert.Throws<ArgumentException>(
            () =>
                new EmployeeWorkScheduleAssignment(
                    Guid.Empty,
                    Guid.NewGuid(),
                    Guid.NewGuid(),
                    Guid.NewGuid(),
                    new DateOnly(
                        2026,
                        8,
                        1)));
    }

    [Fact]
    public void Constructor_WhenEmployeeIdIsEmpty_Throws()
    {
        Assert.Throws<ArgumentException>(
            () =>
                new EmployeeWorkScheduleAssignment(
                    Guid.NewGuid(),
                    Guid.Empty,
                    Guid.NewGuid(),
                    Guid.NewGuid(),
                    new DateOnly(
                        2026,
                        8,
                        1)));
    }

    [Fact]
    public void Constructor_WhenEmploymentPeriodIdIsEmpty_Throws()
    {
        Assert.Throws<ArgumentException>(
            () =>
                new EmployeeWorkScheduleAssignment(
                    Guid.NewGuid(),
                    Guid.NewGuid(),
                    Guid.Empty,
                    Guid.NewGuid(),
                    new DateOnly(
                        2026,
                        8,
                        1)));
    }

    [Fact]
    public void Constructor_WhenWorkScheduleIdIsEmpty_Throws()
    {
        Assert.Throws<ArgumentException>(
            () =>
                new EmployeeWorkScheduleAssignment(
                    Guid.NewGuid(),
                    Guid.NewGuid(),
                    Guid.NewGuid(),
                    Guid.Empty,
                    new DateOnly(
                        2026,
                        8,
                        1)));
    }

    [Fact]
    public void Constructor_WhenEffectiveFromIsDefault_Throws()
    {
        Assert.Throws<ArgumentException>(
            () =>
                new EmployeeWorkScheduleAssignment(
                    Guid.NewGuid(),
                    Guid.NewGuid(),
                    Guid.NewGuid(),
                    Guid.NewGuid(),
                    default));
    }

    [Fact]
    public void Constructor_WhenEffectiveToIsBeforeEffectiveFrom_Throws()
    {
        Assert.Throws<ArgumentException>(
            () =>
                new EmployeeWorkScheduleAssignment(
                    Guid.NewGuid(),
                    Guid.NewGuid(),
                    Guid.NewGuid(),
                    Guid.NewGuid(),
                    new DateOnly(
                        2026,
                        8,
                        10),
                    new DateOnly(
                        2026,
                        8,
                        9)));
    }

    [Fact]
    public void Close_WhenValid_ClosesAssignment()
    {
        var assignment =
            new EmployeeWorkScheduleAssignment(
                Guid.NewGuid(),
                Guid.NewGuid(),
                Guid.NewGuid(),
                Guid.NewGuid(),
                new DateOnly(
                    2026,
                    8,
                    1));

        DateOnly effectiveTo =
            new(
                2026,
                8,
                31);

        assignment.Close(
            effectiveTo);

        Assert.False(
            assignment.IsOpen);

        Assert.Equal(
            effectiveTo,
            assignment.EffectiveTo);
    }

    [Fact]
    public void Close_WhenAlreadyClosed_Throws()
    {
        var assignment =
            new EmployeeWorkScheduleAssignment(
                Guid.NewGuid(),
                Guid.NewGuid(),
                Guid.NewGuid(),
                Guid.NewGuid(),
                new DateOnly(
                    2026,
                    8,
                    1));

        assignment.Close(
            new DateOnly(
                2026,
                8,
                31));

        Assert.Throws<InvalidOperationException>(
            () =>
                assignment.Close(
                    new DateOnly(
                        2026,
                        9,
                        30)));
    }
}
