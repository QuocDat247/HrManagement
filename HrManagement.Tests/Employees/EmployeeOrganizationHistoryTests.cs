using HrManagement.Domain.Employees.OrganizationAssignments;
using Xunit;

namespace HrManagement.Tests.Employees;

public sealed class EmployeeOrganizationHistoryTests
{
    [Fact]
    public void Constructor_SortsAssignmentsAndFindsCurrent()
    {
        Guid employeeId =
            Guid.NewGuid();

        Guid employmentPeriodId =
            Guid.NewGuid();

        EmployeeOrganizationAssignment first =
            CreateAssignment(
                employeeId,
                employmentPeriodId,
                new DateOnly(2025, 1, 1),
                new DateOnly(2025, 5, 31));

        EmployeeOrganizationAssignment second =
            CreateAssignment(
                employeeId,
                employmentPeriodId,
                new DateOnly(2025, 6, 1));

        var history =
            new EmployeeOrganizationHistory(
                employeeId,
                [second, first]);

        Assert.Equal(
            first.Id,
            history.Assignments[0].Id);

        Assert.Equal(
            second.Id,
            history.Assignments[1].Id);

        Assert.Same(
            second,
            history.CurrentAssignment);

        Assert.Same(
            second,
            history.LatestAssignment);
    }

    [Fact]
    public void Constructor_WhenAssignmentsOverlap_Throws()
    {
        Guid employeeId =
            Guid.NewGuid();

        Guid employmentPeriodId =
            Guid.NewGuid();

        EmployeeOrganizationAssignment first =
            CreateAssignment(
                employeeId,
                employmentPeriodId,
                new DateOnly(2025, 1, 1),
                new DateOnly(2025, 6, 30));

        EmployeeOrganizationAssignment second =
            CreateAssignment(
                employeeId,
                employmentPeriodId,
                new DateOnly(2025, 6, 1));

        Assert.Throws<ArgumentException>(
            () =>
                new EmployeeOrganizationHistory(
                    employeeId,
                    [first, second]));
    }

    [Fact]
    public void Transfer_ClosesCurrentAndStartsNewAssignment()
    {
        Guid employeeId =
            Guid.NewGuid();

        Guid employmentPeriodId =
            Guid.NewGuid();

        EmployeeOrganizationAssignment current =
            CreateAssignment(
                employeeId,
                employmentPeriodId,
                new DateOnly(2025, 1, 1));

        var history =
            new EmployeeOrganizationHistory(
                employeeId,
                [current]);

        Guid targetDepartmentId =
            Guid.NewGuid();

        Guid targetPositionId =
            Guid.NewGuid();

        EmployeeOrganizationAssignment transferred =
            history.Transfer(
                Guid.NewGuid(),
                targetDepartmentId,
                "RD",
                "Nghiên cứu và phát triển",
                targetPositionId,
                "LEAD",
                "Trưởng nhóm kỹ thuật",
                new DateOnly(2025, 6, 1));

        Assert.Equal(
            new DateOnly(2025, 5, 31),
            current.EndDate);

        Assert.Equal(
            new DateOnly(2025, 6, 1),
            transferred.StartDate);

        Assert.Equal(
            employmentPeriodId,
            transferred.EmploymentPeriodId);

        Assert.Equal(
            targetDepartmentId,
            transferred.DepartmentId);

        Assert.Equal(
            targetPositionId,
            transferred.PositionId);

        Assert.Same(
            transferred,
            history.CurrentAssignment);
    }

    [Fact]
    public void Transfer_WhenInvalid_Throws()
    {
        Guid employeeId =
            Guid.NewGuid();

        Guid employmentPeriodId =
            Guid.NewGuid();

        EmployeeOrganizationAssignment current =
            CreateAssignment(
                employeeId,
                employmentPeriodId,
                new DateOnly(2025, 1, 1));

        var history =
            new EmployeeOrganizationHistory(
                employeeId,
                [current]);

        Assert.Throws<InvalidOperationException>(
            () =>
                history.Transfer(
                    Guid.NewGuid(),
                    current.DepartmentId,
                    current.DepartmentCode,
                    current.DepartmentName,
                    current.PositionId,
                    current.PositionCode,
                    current.PositionName,
                    new DateOnly(2025, 6, 1)));

        Assert.Throws<ArgumentException>(
            () =>
                history.Transfer(
                    Guid.NewGuid(),
                    Guid.NewGuid(),
                    "RD",
                    "Nghiên cứu",
                    Guid.NewGuid(),
                    "LEAD",
                    "Trưởng nhóm",
                    current.StartDate));
    }

    [Fact]
    public void CloseThenReopenLatestAssignment_RestoresCurrent()
    {
        Guid employeeId =
            Guid.NewGuid();

        EmployeeOrganizationAssignment assignment =
            CreateAssignment(
                employeeId,
                Guid.NewGuid(),
                new DateOnly(2025, 1, 1));

        var history =
            new EmployeeOrganizationHistory(
                employeeId,
                [assignment]);

        DateOnly terminationDate =
            new DateOnly(2025, 8, 31);

        history.CloseCurrentAssignment(
            terminationDate);

        Assert.Null(
            history.CurrentAssignment);

        history.ReopenLatestAssignment(
            terminationDate);

        Assert.Same(
            assignment,
            history.CurrentAssignment);

        Assert.True(
            assignment.IsOpen);
    }

    [Fact]
    public void StartNewAssignment_AfterClosedHistory_StartsNewEmploymentPeriodAssignment()
    {
        Guid employeeId =
            Guid.NewGuid();

        Guid oldEmploymentPeriodId =
            Guid.NewGuid();

        EmployeeOrganizationAssignment previous =
            CreateAssignment(
                employeeId,
                oldEmploymentPeriodId,
                new DateOnly(2024, 1, 1),
                new DateOnly(2025, 1, 31));

        var history =
            new EmployeeOrganizationHistory(
                employeeId,
                [previous]);

        Guid newEmploymentPeriodId =
            Guid.NewGuid();

        EmployeeOrganizationAssignment current =
            history.StartNewAssignment(
                Guid.NewGuid(),
                newEmploymentPeriodId,
                Guid.NewGuid(),
                "DEV",
                "Phát triển phần mềm",
                Guid.NewGuid(),
                "SWE",
                "Kỹ sư phần mềm",
                new DateOnly(2025, 2, 10));

        Assert.Equal(
            newEmploymentPeriodId,
            current.EmploymentPeriodId);

        Assert.Equal(
            new DateOnly(2025, 2, 10),
            current.StartDate);

        Assert.True(
            current.IsOpen);

        Assert.Same(
            current,
            history.CurrentAssignment);

        Assert.Equal(
            2,
            history.Assignments.Count);
    }

    private static EmployeeOrganizationAssignment
        CreateAssignment(
            Guid employeeId,
            Guid employmentPeriodId,
            DateOnly startDate,
            DateOnly? endDate = null)
    {
        return new EmployeeOrganizationAssignment(
            Guid.NewGuid(),
            employeeId,
            employmentPeriodId,
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
