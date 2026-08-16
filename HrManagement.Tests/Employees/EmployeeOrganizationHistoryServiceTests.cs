using HrManagement.Application.Employees.OrganizationAssignments;
using HrManagement.Domain.Employees.OrganizationAssignments;

namespace HrManagement.Tests.Employees;

public sealed class EmployeeOrganizationHistoryServiceTests
{
    [Fact]
    public async Task GetHistoryAsync_MapsOrderedAssignmentsAndSnapshots()
    {
        Guid employeeId =
            Guid.NewGuid();

        Guid employmentPeriodId =
            Guid.NewGuid();

        var first =
            new EmployeeOrganizationAssignment(
                Guid.NewGuid(),
                employeeId,
                employmentPeriodId,
                Guid.NewGuid(),
                "DEV",
                "Phát triển phần mềm cũ",
                Guid.NewGuid(),
                "SWE",
                "Kỹ sư phần mềm cũ",
                new DateOnly(2025, 1, 1),
                new DateOnly(2025, 5, 31),
                isBaseline: true);

        var second =
            new EmployeeOrganizationAssignment(
                Guid.NewGuid(),
                employeeId,
                employmentPeriodId,
                Guid.NewGuid(),
                "RD",
                "Nghiên cứu và phát triển",
                Guid.NewGuid(),
                "LEAD",
                "Trưởng nhóm kỹ thuật",
                new DateOnly(2025, 6, 1));

        var repository =
            new StubHistoryRepository
            {
                History =
                    new EmployeeOrganizationHistory(
                        employeeId,
                        [second, first])
            };

        var service =
            new EmployeeOrganizationHistoryService(
                repository);

        EmployeeOrganizationAssignmentHistoryDetails result =
            await service.GetHistoryAsync(
                employeeId);

        Assert.Equal(
            employeeId,
            result.EmployeeId);

        Assert.Equal(
            2,
            result.Assignments.Count);

        OrganizationAssignmentHistoryItem firstItem =
            result.Assignments[0];

        OrganizationAssignmentHistoryItem secondItem =
            result.Assignments[1];

        Assert.Equal(
            1,
            firstItem.SequenceNumber);

        Assert.Equal(
            first.Id,
            firstItem.Id);

        Assert.Equal(
            "Phát triển phần mềm cũ",
            firstItem.DepartmentName);

        Assert.Equal(
            "Kỹ sư phần mềm cũ",
            firstItem.PositionName);

        Assert.True(
            firstItem.IsBaseline);

        Assert.False(
            firstItem.IsOpen);

        Assert.Equal(
            2,
            secondItem.SequenceNumber);

        Assert.Equal(
            second.Id,
            secondItem.Id);

        Assert.Equal(
            "Nghiên cứu và phát triển",
            secondItem.DepartmentName);

        Assert.Equal(
            "Trưởng nhóm kỹ thuật",
            secondItem.PositionName);

        Assert.False(
            secondItem.IsBaseline);

        Assert.True(
            secondItem.IsOpen);
    }

    [Fact]
    public async Task GetHistoryAsync_WhenHistoryIsEmpty_ReturnsEmptyAssignments()
    {
        Guid employeeId =
            Guid.NewGuid();

        var repository =
            new StubHistoryRepository
            {
                History =
                    new EmployeeOrganizationHistory(
                        employeeId,
                        [])
            };

        var service =
            new EmployeeOrganizationHistoryService(
                repository);

        EmployeeOrganizationAssignmentHistoryDetails result =
            await service.GetHistoryAsync(
                employeeId);

        Assert.Equal(
            employeeId,
            result.EmployeeId);

        Assert.Empty(
            result.Assignments);
    }

    [Fact]
    public async Task GetHistoryAsync_WhenEmployeeIdIsEmpty_Throws()
    {
        var service =
            new EmployeeOrganizationHistoryService(
                new StubHistoryRepository());

        await Assert.ThrowsAsync<ArgumentException>(
            () =>
                service.GetHistoryAsync(
                    Guid.Empty));
    }

    private sealed class StubHistoryRepository
        : IEmployeeOrganizationHistoryRepository
    {
        public EmployeeOrganizationHistory? History
        {
            get;
            set;
        }

        public Task<EmployeeOrganizationHistory>
            GetByEmployeeIdAsync(
                Guid employeeId,
                CancellationToken cancellationToken = default)
        {
            return Task.FromResult(
                History
                ?? new EmployeeOrganizationHistory(
                    employeeId,
                    []));
        }

        public Task AddAssignmentAsync(
            EmployeeOrganizationAssignment assignment,
            CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public Task UpdateAssignmentAsync(
            EmployeeOrganizationAssignment assignment,
            CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }
}
