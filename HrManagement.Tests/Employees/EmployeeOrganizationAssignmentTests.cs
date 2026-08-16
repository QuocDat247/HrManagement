using HrManagement.Domain.Employees.OrganizationAssignments;
using Xunit;

namespace HrManagement.Tests.Employees;

public sealed class EmployeeOrganizationAssignmentTests
{
    [Fact]
    public void Constructor_WithValidData_CreatesOpenAssignment()
    {
        Guid id =
            Guid.NewGuid();

        Guid employeeId =
            Guid.NewGuid();

        Guid employmentPeriodId =
            Guid.NewGuid();

        Guid departmentId =
            Guid.NewGuid();

        Guid positionId =
            Guid.NewGuid();

        var assignment =
            new EmployeeOrganizationAssignment(
                id,
                employeeId,
                employmentPeriodId,
                departmentId,
                " DEV ",
                " Phát triển phần mềm ",
                positionId,
                " SWE ",
                " Kỹ sư phần mềm ",
                new DateOnly(2025, 1, 1));

        Assert.Equal(
            id,
            assignment.Id);

        Assert.Equal(
            employmentPeriodId,
            assignment.EmploymentPeriodId);

        Assert.Equal(
            "DEV",
            assignment.DepartmentCode);

        Assert.Equal(
            "Phát triển phần mềm",
            assignment.DepartmentName);

        Assert.Equal(
            "SWE",
            assignment.PositionCode);

        Assert.Equal(
            "Kỹ sư phần mềm",
            assignment.PositionName);

        Assert.True(
            assignment.IsOpen);

        Assert.Null(
            assignment.EndDate);

        Assert.False(
        assignment.IsBaseline);
    }

    [Fact]
    public void Constructor_WhenEndBeforeStart_Throws()
    {
        Assert.Throws<ArgumentException>(
            () =>
                CreateAssignment(
                    startDate:
                        new DateOnly(2025, 2, 1),
                    endDate:
                        new DateOnly(2025, 1, 31)));
    }

    [Fact]
    public void Close_WhenValid_ClosesAssignment()
    {
        EmployeeOrganizationAssignment assignment =
            CreateAssignment(
                startDate:
                    new DateOnly(2025, 1, 1));

        assignment.Close(
            new DateOnly(2025, 5, 31));

        Assert.False(
            assignment.IsOpen);

        Assert.Equal(
            new DateOnly(2025, 5, 31),
            assignment.EndDate);
    }

    [Fact]
    public void Close_WhenAlreadyClosed_Throws()
    {
        EmployeeOrganizationAssignment assignment =
            CreateAssignment(
                startDate:
                    new DateOnly(2025, 1, 1));

        assignment.Close(
            new DateOnly(2025, 5, 31));

        Assert.Throws<InvalidOperationException>(
            () =>
                assignment.Close(
                    new DateOnly(2025, 6, 30)));
    }

    private static EmployeeOrganizationAssignment
        CreateAssignment(
            DateOnly startDate,
            DateOnly? endDate = null)
    {
        return new EmployeeOrganizationAssignment(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "DEV",
            "Phát triển phần mềm",
            Guid.NewGuid(),
            "SWE",
            "Kỹ sư phần mềm",
            startDate,
            endDate);
    }
}
