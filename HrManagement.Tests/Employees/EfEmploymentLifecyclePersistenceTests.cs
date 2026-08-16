using HrManagement.Application.Employees.EmploymentLifecycle;
using HrManagement.Domain.Employees;
using HrManagement.Infrastructure.Employees;
using HrManagement.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using HrManagement.Domain.Employees.OrganizationAssignments;
using HrManagement.Domain.Organization.Departments;
using HrManagement.Domain.Organization.Positions;
using static HrManagement.Tests.Employees.EfEmploymentHistoryRepositoryTests;

namespace HrManagement.Tests.Employees;
public sealed class EfEmploymentLifecyclePersistenceTests
{
    [Fact]
    public async Task CreateEmployeeWithPeriodAndAssignmentAsync_WhenAssignmentCannotBeInserted_RollsBackEmployeeAndPeriod()
    {
        await using var connection =
            new SqliteConnection(
                "Data Source=:memory:;Foreign Keys=True");

        await connection.OpenAsync();

        DbContextOptions<HrManagementDbContext> options =
            new DbContextOptionsBuilder<HrManagementDbContext>()
                .UseSqlite(connection)
                .Options;

        var organization =
            await SeedOrganizationAsync(
                options);

        DateOnly hireDate =
            new(2026, 8, 1);

        Guid existingEmployeeId =
            Guid.NewGuid();

        Guid existingPeriodId =
            Guid.NewGuid();

        Guid duplicateAssignmentId =
            Guid.NewGuid();

        // Seed một lifecycle hợp lệ trước,
        // để lấy assignment Id gây duplicate PK.
        await using (var dbContext =
                     new HrManagementDbContext(options))
        {
            Employee existingEmployee =
                CreateEmployeeWithOrganization(
                    existingEmployeeId,
                    "EMP-CREATE-EXISTING",
                    hireDate,
                    EmployeeStatus.Active,
                    organization.Department,
                    organization.Position);

            var existingPeriod =
                new EmploymentPeriod(
                    existingPeriodId,
                    existingEmployeeId,
                    hireDate);

            var existingAssignment =
                new EmployeeOrganizationAssignment(
                    duplicateAssignmentId,
                    existingEmployeeId,
                    existingPeriodId,
                    organization.Department.Id,
                    organization.Department.Code,
                    organization.Department.Name,
                    organization.Position.Id,
                    organization.Position.Code,
                    organization.Position.Name,
                    hireDate);

            dbContext.Employees.Add(
                existingEmployee);

            dbContext.EmploymentPeriods.Add(
                existingPeriod);

            dbContext.EmployeeOrganizationAssignments.Add(
                existingAssignment);

            await dbContext.SaveChangesAsync();
        }

        // Lifecycle mới hoàn toàn hợp lệ...
        Guid newEmployeeId =
            Guid.NewGuid();

        Employee newEmployee =
            CreateEmployeeWithOrganization(
                newEmployeeId,
                "EMP-CREATE-ROLLBACK",
                hireDate,
                EmployeeStatus.Active,
                organization.Department,
                organization.Position);

        var newPeriod =
            new EmploymentPeriod(
                Guid.NewGuid(),
                newEmployeeId,
                hireDate);

        // ...nhưng cố tình dùng lại assignment PK cũ.
        var invalidAssignment =
            new EmployeeOrganizationAssignment(
                duplicateAssignmentId,
                newEmployeeId,
                newPeriod.Id,
                organization.Department.Id,
                organization.Department.Code,
                organization.Department.Name,
                organization.Position.Id,
                organization.Position.Code,
                organization.Position.Name,
                hireDate);

        var persistence =
            new EfEmploymentLifecyclePersistence(
                new TestDbContextFactory(options));

        await Assert.ThrowsAsync<DbUpdateException>(
            () =>
                persistence
                    .CreateEmployeeWithPeriodAndAssignmentAsync(
                        newEmployee,
                        newPeriod,
                        invalidAssignment));

        await using var verificationContext =
            new HrManagementDbContext(options);

        List<Employee> employees =
            await verificationContext
                .Employees
                .ToListAsync();

        List<EmploymentPeriod> periods =
            await verificationContext
                .EmploymentPeriods
                .ToListAsync();

        List<EmployeeOrganizationAssignment> assignments =
            await verificationContext
                .EmployeeOrganizationAssignments
                .ToListAsync();

        // Employee mới phải rollback.
        Assert.DoesNotContain(
            employees,
            employee =>
                employee.Id == newEmployeeId);

        // Period mới cũng phải rollback.
        Assert.DoesNotContain(
            periods,
            period =>
                period.Id == newPeriod.Id);

        // Assignment seed cũ vẫn còn nguyên,
        // assignment mới không được tạo.
        EmployeeOrganizationAssignment persistedAssignment =
            Assert.Single(
                assignments);

        Assert.Equal(
            duplicateAssignmentId,
            persistedAssignment.Id);

        Assert.Equal(
            existingEmployeeId,
            persistedAssignment.EmployeeId);

        Assert.Equal(
            existingPeriodId,
            persistedAssignment.EmploymentPeriodId);

        Assert.True(
            persistedAssignment.IsOpen);
    }

    [Fact]
    public async Task UpdateEmployeeWithPeriodAndAssignmentAsync_WhenSaveFails_RollsBackAllChanges()
    {
        await using var connection =
            new SqliteConnection(
                "Data Source=:memory:");

        await connection.OpenAsync();

        DbContextOptions<HrManagementDbContext> options =
            new DbContextOptionsBuilder<HrManagementDbContext>()
                .UseSqlite(connection)
                .Options;

        var organization =
            await SeedOrganizationAsync(
                options);

        Guid targetEmployeeId =
            Guid.NewGuid();

        Guid otherEmployeeId =
            Guid.NewGuid();

        Guid periodId =
            Guid.NewGuid();

        Guid assignmentId =
            Guid.NewGuid();

        DateOnly hireDate =
            new(2025, 1, 10);

        await using (var dbContext =
             new HrManagementDbContext(options))
        {
            dbContext.Employees.AddRange(
                CreateEmployeeWithOrganization(
                    targetEmployeeId,
                    "EMP-ATOMIC-A",
                    hireDate,
                    EmployeeStatus.Active,
                    organization.Department,
                    organization.Position),

                CreateEmployeeWithOrganization(
                    otherEmployeeId,
                    "EMP-ATOMIC-B",
                    hireDate,
                    EmployeeStatus.Active,
                    organization.Department,
                    organization.Position));

            dbContext.EmploymentPeriods.Add(
                new EmploymentPeriod(
                    periodId,
                    targetEmployeeId,
                    hireDate));

            dbContext.EmployeeOrganizationAssignments.Add(
                new EmployeeOrganizationAssignment(
                    assignmentId,
                    targetEmployeeId,
                    periodId,
                    organization.Department.Id,
                    organization.Department.Code,
                    organization.Department.Name,
                    organization.Position.Id,
                    organization.Position.Code,
                    organization.Position.Name,
                    hireDate));

            await dbContext.SaveChangesAsync();
        }

        DateOnly terminationDate =
            new(2026, 8, 12);

        Employee invalidEmployee =
            CreateEmployeeWithOrganization(
                targetEmployeeId,
                "EMP-ATOMIC-B",
                hireDate,
                EmployeeStatus.Inactive,
                organization.Department,
                organization.Position,
                terminationDate);

        var closedPeriod =
            new EmploymentPeriod(
                periodId,
                targetEmployeeId,
                hireDate,
                terminationDate);

        var closedAssignment =
            new EmployeeOrganizationAssignment(
                assignmentId,
                targetEmployeeId,
                periodId,
                organization.Department.Id,
                organization.Department.Code,
                organization.Department.Name,
                organization.Position.Id,
                organization.Position.Code,
                organization.Position.Name,
                hireDate,
                terminationDate);

        var persistence =
            new EfEmploymentLifecyclePersistence(
                new TestDbContextFactory(options));

        await Assert.ThrowsAsync<DbUpdateException>(
            () =>
                persistence
                    .UpdateEmployeeWithPeriodAndAssignmentAsync(
                        invalidEmployee,
                        closedPeriod,
                        closedAssignment));

        await using var verificationContext =
            new HrManagementDbContext(options);

        Employee originalEmployee =
            await verificationContext.Employees
                .SingleAsync(
                    employee =>
                        employee.Id == targetEmployeeId);

        EmploymentPeriod originalPeriod =
            await verificationContext.EmploymentPeriods
                .SingleAsync(
                    period =>
                        period.Id == periodId);

        EmployeeOrganizationAssignment originalAssignment =
            await verificationContext
                .EmployeeOrganizationAssignments
                .SingleAsync(
                    assignment =>
                        assignment.Id == assignmentId);

        Assert.Equal(
            "EMP-ATOMIC-A",
            originalEmployee.EmployeeCode);

        Assert.Equal(
            EmployeeStatus.Active,
            originalEmployee.Status);

        Assert.Null(
            originalEmployee.TerminationDate);

        Assert.True(
            originalPeriod.IsOpen);

        Assert.Null(
            originalPeriod.EndDate);

        Assert.True(
            originalAssignment.IsOpen);

        Assert.Null(
            originalAssignment.EndDate);
    }

    private static Employee CreateEmployee(
    Guid id,
    string employeeCode,
    DateOnly hireDate,
    EmployeeStatus status,
    DateOnly? terminationDate = null)
    {
        return new Employee(
            id,
            employeeCode,
            $"Nhân viên {employeeCode}",
            "employee@example.com",
            "0901000000",
            new DateOnly(1995, 1, 1),
            hireDate,
            "Kiểm thử",
            "Nhân viên",
            status,
            terminationDate);
    }

    private sealed class TestDbContextFactory
        : IDbContextFactory<HrManagementDbContext>
    {
        private readonly DbContextOptions<HrManagementDbContext>
            _options;

        public TestDbContextFactory(
            DbContextOptions<HrManagementDbContext> options)
        {
            _options = options;
        }

        public HrManagementDbContext CreateDbContext()
        {
            return new HrManagementDbContext(
                _options);
        }
    }

    private static async Task<(Department Department, Position Position)>
    SeedOrganizationAsync(
        DbContextOptions<HrManagementDbContext> options)
    {
        var department =
            new Department(
                Guid.NewGuid(),
                "DEV",
                "Phát triển phần mềm");

        var position =
            new Position(
                Guid.NewGuid(),
                "SWE",
                "Kỹ sư phần mềm");

        await using var dbContext =
            new HrManagementDbContext(
                options);

        await dbContext.Database
            .EnsureCreatedAsync();

        dbContext.Departments.Add(
            department);

        dbContext.Positions.Add(
            position);

        await dbContext.SaveChangesAsync();

        return (
            department,
            position);
    }

    private static Employee CreateEmployeeWithOrganization(
        Guid id,
        string employeeCode,
        DateOnly hireDate,
        EmployeeStatus status,
        Department department,
        Position position,
        DateOnly? terminationDate = null)
    {
        return new Employee(
            id,
            employeeCode,
            $"Nhân viên {employeeCode}",
            "employee@example.com",
            "0901000000",
            new DateOnly(1995, 1, 1),
            hireDate,
            department.Name,
            position.Name,
            status,
            terminationDate,
            departmentId: department.Id,
            positionId: position.Id);
    }

    [Fact]
    public async Task CreateEmployeeWithPeriodAndAssignmentAsync_PersistsAllThreeEntities()
    {
        await using var connection =
            new SqliteConnection(
                "Data Source=:memory:;Foreign Keys=True");

        await connection.OpenAsync();

        DbContextOptions<HrManagementDbContext> options =
            new DbContextOptionsBuilder<HrManagementDbContext>()
                .UseSqlite(connection)
                .Options;

        var organization =
            await SeedOrganizationAsync(
                options);

        DateOnly hireDate =
            new(2026, 8, 1);

        Employee employee =
            CreateEmployeeWithOrganization(
                Guid.NewGuid(),
                "EMP-LC-ORG-001",
                hireDate,
                EmployeeStatus.Active,
                organization.Department,
                organization.Position);

        var period =
            new EmploymentPeriod(
                Guid.NewGuid(),
                employee.Id,
                hireDate);

        var assignment =
            new EmployeeOrganizationAssignment(
                Guid.NewGuid(),
                employee.Id,
                period.Id,
                organization.Department.Id,
                organization.Department.Code,
                organization.Department.Name,
                organization.Position.Id,
                organization.Position.Code,
                organization.Position.Name,
                hireDate);

        var persistence =
            new EfEmploymentLifecyclePersistence(
                new TestDbContextFactory(options));

        await persistence
            .CreateEmployeeWithPeriodAndAssignmentAsync(
                employee,
                period,
                assignment);

        await using var verificationContext =
            new HrManagementDbContext(options);

        Assert.Equal(
            1,
            await verificationContext.Employees.CountAsync());

        Assert.Equal(
            1,
            await verificationContext.EmploymentPeriods.CountAsync());

        EmployeeOrganizationAssignment persistedAssignment =
            await verificationContext
                .EmployeeOrganizationAssignments
                .SingleAsync();

        Assert.Equal(
            employee.Id,
            persistedAssignment.EmployeeId);

        Assert.Equal(
            period.Id,
            persistedAssignment.EmploymentPeriodId);

        Assert.False(
            persistedAssignment.IsBaseline);

        Assert.True(
            persistedAssignment.IsOpen);
    }

    [Fact]
    public async Task UpdateEmployeeWithPeriodAndAssignmentAsync_PersistsClosedLifecycle()
    {
        await using var connection =
            new SqliteConnection(
                "Data Source=:memory:;Foreign Keys=True");

        await connection.OpenAsync();

        DbContextOptions<HrManagementDbContext> options =
            new DbContextOptionsBuilder<HrManagementDbContext>()
                .UseSqlite(connection)
                .Options;

        var organization =
            await SeedOrganizationAsync(
                options);

        Guid employeeId =
            Guid.NewGuid();

        Guid periodId =
            Guid.NewGuid();

        Guid assignmentId =
            Guid.NewGuid();

        DateOnly hireDate =
            new(2025, 1, 10);

        await using (var dbContext =
                     new HrManagementDbContext(options))
        {
            dbContext.Employees.Add(
                CreateEmployeeWithOrganization(
                    employeeId,
                    "EMP-LC-ORG-002",
                    hireDate,
                    EmployeeStatus.Active,
                    organization.Department,
                    organization.Position));

            dbContext.EmploymentPeriods.Add(
                new EmploymentPeriod(
                    periodId,
                    employeeId,
                    hireDate));

            dbContext.EmployeeOrganizationAssignments.Add(
                new EmployeeOrganizationAssignment(
                    assignmentId,
                    employeeId,
                    periodId,
                    organization.Department.Id,
                    organization.Department.Code,
                    organization.Department.Name,
                    organization.Position.Id,
                    organization.Position.Code,
                    organization.Position.Name,
                    hireDate));

            await dbContext.SaveChangesAsync();
        }

        DateOnly terminationDate =
            new(2026, 8, 10);

        Employee inactiveEmployee =
            CreateEmployeeWithOrganization(
                employeeId,
                "EMP-LC-ORG-002",
                hireDate,
                EmployeeStatus.Inactive,
                organization.Department,
                organization.Position,
                terminationDate);

        var closedPeriod =
            new EmploymentPeriod(
                periodId,
                employeeId,
                hireDate,
                terminationDate);

        var closedAssignment =
            new EmployeeOrganizationAssignment(
                assignmentId,
                employeeId,
                periodId,
                organization.Department.Id,
                organization.Department.Code,
                organization.Department.Name,
                organization.Position.Id,
                organization.Position.Code,
                organization.Position.Name,
                hireDate,
                terminationDate);

        var persistence =
            new EfEmploymentLifecyclePersistence(
                new TestDbContextFactory(options));

        await persistence
            .UpdateEmployeeWithPeriodAndAssignmentAsync(
                inactiveEmployee,
                closedPeriod,
                closedAssignment);

        await using var verificationContext =
            new HrManagementDbContext(options);

        Employee persistedEmployee =
            await verificationContext.Employees.SingleAsync();

        EmploymentPeriod persistedPeriod =
            await verificationContext.EmploymentPeriods.SingleAsync();

        EmployeeOrganizationAssignment persistedAssignment =
            await verificationContext
                .EmployeeOrganizationAssignments
                .SingleAsync();

        Assert.Equal(
            EmployeeStatus.Inactive,
            persistedEmployee.Status);

        Assert.Equal(
            terminationDate,
            persistedPeriod.EndDate);

        Assert.Equal(
            terminationDate,
            persistedAssignment.EndDate);

        Assert.False(
            persistedAssignment.IsOpen);
    }

    [Fact]
    public async Task UpdateEmployeeWithNewPeriodAndAssignmentAsync_PersistsRehireLifecycle()
    {
        await using var connection =
            new SqliteConnection(
                "Data Source=:memory:;Foreign Keys=True");

        await connection.OpenAsync();

        DbContextOptions<HrManagementDbContext> options =
            new DbContextOptionsBuilder<HrManagementDbContext>()
                .UseSqlite(connection)
                .Options;

        var organization =
            await SeedOrganizationAsync(
                options);

        Guid employeeId =
            Guid.NewGuid();

        Guid oldPeriodId =
            Guid.NewGuid();

        DateOnly hireDate =
            new(2024, 1, 1);

        DateOnly terminationDate =
            new(2026, 3, 31);

        await using (var dbContext =
                     new HrManagementDbContext(options))
        {
            dbContext.Employees.Add(
                CreateEmployeeWithOrganization(
                    employeeId,
                    "EMP-LC-ORG-003",
                    hireDate,
                    EmployeeStatus.Inactive,
                    organization.Department,
                    organization.Position,
                    terminationDate));

            dbContext.EmploymentPeriods.Add(
                new EmploymentPeriod(
                    oldPeriodId,
                    employeeId,
                    hireDate,
                    terminationDate));

            dbContext.EmployeeOrganizationAssignments.Add(
                new EmployeeOrganizationAssignment(
                    Guid.NewGuid(),
                    employeeId,
                    oldPeriodId,
                    organization.Department.Id,
                    organization.Department.Code,
                    organization.Department.Name,
                    organization.Position.Id,
                    organization.Position.Code,
                    organization.Position.Name,
                    hireDate,
                    terminationDate));

            await dbContext.SaveChangesAsync();
        }

        DateOnly rehireDate =
            new(2026, 8, 1);

        Employee restoredEmployee =
            CreateEmployeeWithOrganization(
                employeeId,
                "EMP-LC-ORG-003",
                hireDate,
                EmployeeStatus.Active,
                organization.Department,
                organization.Position);

        var newPeriod =
            new EmploymentPeriod(
                Guid.NewGuid(),
                employeeId,
                rehireDate);

        var newAssignment =
            new EmployeeOrganizationAssignment(
                Guid.NewGuid(),
                employeeId,
                newPeriod.Id,
                organization.Department.Id,
                organization.Department.Code,
                organization.Department.Name,
                organization.Position.Id,
                organization.Position.Code,
                organization.Position.Name,
                rehireDate);

        var persistence =
            new EfEmploymentLifecyclePersistence(
                new TestDbContextFactory(options));

        await persistence
            .UpdateEmployeeWithNewPeriodAndAssignmentAsync(
                restoredEmployee,
                newPeriod,
                newAssignment);

        await using var verificationContext =
            new HrManagementDbContext(options);

        Assert.Equal(
            2,
            await verificationContext
                .EmploymentPeriods
                .CountAsync());

        List<EmployeeOrganizationAssignment> assignments =
            await verificationContext
                .EmployeeOrganizationAssignments
                .OrderBy(
                    assignment =>
                        assignment.StartDate)
                .ToListAsync();

        Assert.Equal(
            2,
            assignments.Count);

        EmployeeOrganizationAssignment current =
            assignments[1];

        Assert.Equal(
            newPeriod.Id,
            current.EmploymentPeriodId);

        Assert.Equal(
            rehireDate,
            current.StartDate);

        Assert.True(
            current.IsOpen);

        Assert.False(
            current.IsBaseline);
    }

    [Fact]
    public async Task UpdateEmployeeWithNewPeriodAndAssignmentAsync_WhenNewPeriodCannotBeInserted_RollsBackAllChanges()
    {
        await using var connection =
            new SqliteConnection(
                "Data Source=:memory:");

        await connection.OpenAsync();

        DbContextOptions<HrManagementDbContext> options =
            new DbContextOptionsBuilder<HrManagementDbContext>()
                .UseSqlite(connection)
                .Options;

        var organization =
            await SeedOrganizationAsync(
                options);

        Guid employeeId =
            Guid.NewGuid();

        Guid existingPeriodId =
            Guid.NewGuid();

        Guid previousAssignmentId =
            Guid.NewGuid();

        DateOnly hireDate =
            new(2022, 1, 10);

        DateOnly terminationDate =
            new(2026, 3, 15);

        await using (var dbContext =
             new HrManagementDbContext(options))
        {
            dbContext.Employees.Add(
                CreateEmployeeWithOrganization(
                    employeeId,
                    "EMP-REHIRE-ROLLBACK",
                    hireDate,
                    EmployeeStatus.Inactive,
                    organization.Department,
                    organization.Position,
                    terminationDate));

            dbContext.EmploymentPeriods.Add(
                new EmploymentPeriod(
                    existingPeriodId,
                    employeeId,
                    hireDate,
                    terminationDate));

            dbContext.EmployeeOrganizationAssignments.Add(
                new EmployeeOrganizationAssignment(
                    previousAssignmentId,
                    employeeId,
                    existingPeriodId,
                    organization.Department.Id,
                    organization.Department.Code,
                    organization.Department.Name,
                    organization.Position.Id,
                    organization.Position.Code,
                    organization.Position.Name,
                    hireDate,
                    terminationDate));

            await dbContext.SaveChangesAsync();
        }

        Employee restoredEmployee =
            CreateEmployeeWithOrganization(
                employeeId,
                "EMP-REHIRE-ROLLBACK",
                hireDate,
                EmployeeStatus.Active,
                organization.Department,
                organization.Position);

        // Cố tình trùng PK với period cũ.
        DateOnly rehireDate =
            new(2026, 8, 12);

        var invalidNewPeriod =
            new EmploymentPeriod(
                existingPeriodId,
                employeeId,
                rehireDate);

        var newAssignment =
            new EmployeeOrganizationAssignment(
                Guid.NewGuid(),
                employeeId,
                invalidNewPeriod.Id,
                organization.Department.Id,
                organization.Department.Code,
                organization.Department.Name,
                organization.Position.Id,
                organization.Position.Code,
                organization.Position.Name,
                rehireDate);

        var persistence =
            new EfEmploymentLifecyclePersistence(
                new TestDbContextFactory(options));

        await Assert.ThrowsAsync<DbUpdateException>(
            () =>
                persistence
                    .UpdateEmployeeWithNewPeriodAndAssignmentAsync(
                        restoredEmployee,
                        invalidNewPeriod,
                        newAssignment));

        await using var verificationContext =
            new HrManagementDbContext(options);

        Employee persistedEmployee =
            await verificationContext.Employees
                .SingleAsync();

        List<EmploymentPeriod> periods =
            await verificationContext
                .EmploymentPeriods
                .ToListAsync();

        List<EmployeeOrganizationAssignment> assignments =
            await verificationContext
                .EmployeeOrganizationAssignments
                .ToListAsync();

        Assert.Equal(
            EmployeeStatus.Inactive,
            persistedEmployee.Status);

        Assert.Equal(
            terminationDate,
            persistedEmployee.TerminationDate);

        EmploymentPeriod persistedPeriod =
            Assert.Single(
                periods);

        Assert.Equal(
            existingPeriodId,
            persistedPeriod.Id);

        Assert.Equal(
            terminationDate,
            persistedPeriod.EndDate);

        EmployeeOrganizationAssignment persistedAssignment =
            Assert.Single(
                assignments);

        Assert.Equal(
            previousAssignmentId,
            persistedAssignment.Id);

        Assert.Equal(
            terminationDate,
            persistedAssignment.EndDate);

        Assert.False(
            persistedAssignment.IsOpen);
    }
}
