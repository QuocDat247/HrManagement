using HrManagement.Domain.Employees;
using HrManagement.Domain.Employees.OrganizationAssignments;
using HrManagement.Domain.Organization.Departments;
using HrManagement.Domain.Organization.Positions;
using HrManagement.Infrastructure.Employees;
using HrManagement.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace HrManagement.Tests.Employees;

public sealed class
    EfEmployeeOrganizationTransferPersistenceTests
{
    [Fact]
    public async Task TransferEmployeeOrganizationAsync_PersistsEmployeeAndAssignmentTimeline()
    {
        await using SqliteConnection connection =
            await CreateOpenConnectionAsync();

        DbContextOptions<HrManagementDbContext> options =
            CreateOptions(
                connection);

        SeedData seed =
            await SeedAsync(
                options);

        DateOnly effectiveDate =
            new(2026, 8, 10);

        seed.CurrentAssignment.Close(
            effectiveDate.AddDays(-1));

        var newAssignment =
            new EmployeeOrganizationAssignment(
                Guid.NewGuid(),
                seed.Employee.Id,
                seed.EmploymentPeriod.Id,
                seed.TargetDepartment.Id,
                seed.TargetDepartment.Code,
                seed.TargetDepartment.Name,
                seed.TargetPosition.Id,
                seed.TargetPosition.Code,
                seed.TargetPosition.Name,
                effectiveDate);

        Employee transferredEmployee =
            CreateEmployee(
                seed.Employee.Id,
                seed.Employee.EmployeeCode,
                seed.Employee.HireDate,
                seed.TargetDepartment,
                seed.TargetPosition);

        var persistence =
            new EfEmployeeOrganizationTransferPersistence(
                new TestDbContextFactory(
                    options));

        await persistence
            .TransferEmployeeOrganizationAsync(
                transferredEmployee,
                seed.CurrentAssignment,
                newAssignment);

        await using var verificationContext =
            new HrManagementDbContext(
                options);

        Employee persistedEmployee =
            await verificationContext
                .Employees
                .SingleAsync();

        List<EmployeeOrganizationAssignment> assignments =
            await verificationContext
                .EmployeeOrganizationAssignments
                .OrderBy(
                    assignment =>
                        assignment.StartDate)
                .ToListAsync();

        Assert.Equal(
            seed.TargetDepartment.Id,
            persistedEmployee.DepartmentId);

        Assert.Equal(
            seed.TargetPosition.Id,
            persistedEmployee.PositionId);

        Assert.Equal(
            seed.TargetDepartment.Name,
            persistedEmployee.Department);

        Assert.Equal(
            seed.TargetPosition.Name,
            persistedEmployee.Position);

        Assert.Equal(
            2,
            assignments.Count);

        EmployeeOrganizationAssignment previous =
            assignments[0];

        EmployeeOrganizationAssignment current =
            assignments[1];

        Assert.Equal(
            seed.CurrentAssignment.Id,
            previous.Id);

        Assert.Equal(
            effectiveDate.AddDays(-1),
            previous.EndDate);

        Assert.False(
            previous.IsOpen);

        Assert.Equal(
            effectiveDate,
            current.StartDate);

        Assert.Null(
            current.EndDate);

        Assert.True(
            current.IsOpen);

        Assert.Equal(
            seed.EmploymentPeriod.Id,
            current.EmploymentPeriodId);

        Assert.Equal(
            seed.TargetDepartment.Id,
            current.DepartmentId);

        Assert.Equal(
            seed.TargetPosition.Id,
            current.PositionId);

        Assert.False(
            current.IsBaseline);
    }

    [Fact]
    public async Task TransferEmployeeOrganizationAsync_WhenNewAssignmentCannotBeInserted_RollsBackAllChanges()
    {
        await using SqliteConnection connection =
            await CreateOpenConnectionAsync();

        DbContextOptions<HrManagementDbContext> options =
            CreateOptions(
                connection);

        SeedData seed =
            await SeedAsync(
                options);

        Guid otherEmployeeId =
            Guid.NewGuid();

        Guid otherPeriodId =
            Guid.NewGuid();

        Guid duplicateAssignmentId =
            Guid.NewGuid();

        DateOnly hireDate =
            new(2025, 1, 10);

        // Seed một assignment khác để lấy PK
        // mà transfer mới sẽ cố tình dùng lại.
        await using (var dbContext =
                     new HrManagementDbContext(options))
        {
            Employee otherEmployee =
                CreateEmployee(
                    otherEmployeeId,
                    "EMP-TRANSFER-OTHER",
                    hireDate,
                    seed.TargetDepartment,
                    seed.TargetPosition);

            var otherPeriod =
                new EmploymentPeriod(
                    otherPeriodId,
                    otherEmployeeId,
                    hireDate);

            var existingAssignment =
                new EmployeeOrganizationAssignment(
                    duplicateAssignmentId,
                    otherEmployeeId,
                    otherPeriodId,
                    seed.TargetDepartment.Id,
                    seed.TargetDepartment.Code,
                    seed.TargetDepartment.Name,
                    seed.TargetPosition.Id,
                    seed.TargetPosition.Code,
                    seed.TargetPosition.Name,
                    hireDate);

            dbContext.Employees.Add(
                otherEmployee);

            dbContext.EmploymentPeriods.Add(
                otherPeriod);

            dbContext.EmployeeOrganizationAssignments.Add(
                existingAssignment);

            await dbContext.SaveChangesAsync();
        }

        DateOnly effectiveDate =
            new(2026, 8, 10);

        seed.CurrentAssignment.Close(
            effectiveDate.AddDays(-1));

        Employee transferredEmployee =
            CreateEmployee(
                seed.Employee.Id,
                seed.Employee.EmployeeCode,
                seed.Employee.HireDate,
                seed.TargetDepartment,
                seed.TargetPosition);

        // Cố tình dùng lại PK của assignment
        // thuộc employee khác.
        var invalidNewAssignment =
            new EmployeeOrganizationAssignment(
                duplicateAssignmentId,
                seed.Employee.Id,
                seed.EmploymentPeriod.Id,
                seed.TargetDepartment.Id,
                seed.TargetDepartment.Code,
                seed.TargetDepartment.Name,
                seed.TargetPosition.Id,
                seed.TargetPosition.Code,
                seed.TargetPosition.Name,
                effectiveDate);

        var persistence =
            new EfEmployeeOrganizationTransferPersistence(
                new TestDbContextFactory(
                    options));

        await Assert.ThrowsAsync<DbUpdateException>(
            () =>
                persistence
                    .TransferEmployeeOrganizationAsync(
                        transferredEmployee,
                        seed.CurrentAssignment,
                        invalidNewAssignment));

        await using var verificationContext =
            new HrManagementDbContext(
                options);

        Employee persistedEmployee =
            await verificationContext
                .Employees
                .SingleAsync(
                    employee =>
                        employee.Id ==
                        seed.Employee.Id);

        EmployeeOrganizationAssignment persistedCurrentAssignment =
            await verificationContext
                .EmployeeOrganizationAssignments
                .SingleAsync(
                    assignment =>
                        assignment.Id ==
                        seed.CurrentAssignment.Id);

        Assert.Equal(
            seed.SourceDepartment.Id,
            persistedEmployee.DepartmentId);

        Assert.Equal(
            seed.SourcePosition.Id,
            persistedEmployee.PositionId);

        Assert.Equal(
            seed.SourceDepartment.Name,
            persistedEmployee.Department);

        Assert.Equal(
            seed.SourcePosition.Name,
            persistedEmployee.Position);

        // Update đóng assignment cũ cũng phải rollback.
        Assert.True(
            persistedCurrentAssignment.IsOpen);

        Assert.Null(
            persistedCurrentAssignment.EndDate);

        // Assignment duplicate seed vẫn chỉ có
        // đúng một record.
        Assert.Equal(
            1,
            await verificationContext
                .EmployeeOrganizationAssignments
                .CountAsync(
                    assignment =>
                        assignment.Id ==
                        duplicateAssignmentId));

        Assert.Equal(
            2,
            await verificationContext
                .EmployeeOrganizationAssignments
                .CountAsync());
    }

    [Fact]
    public async Task TransferEmployeeOrganizationAsync_WhenAssignmentsBelongToDifferentEmploymentPeriods_Throws()
    {
        await using SqliteConnection connection =
            await CreateOpenConnectionAsync();

        DbContextOptions<HrManagementDbContext> options =
            CreateOptions(
                connection);

        SeedData seed =
            await SeedAsync(
                options);

        DateOnly effectiveDate =
            new(2026, 8, 10);

        seed.CurrentAssignment.Close(
            effectiveDate.AddDays(-1));

        Employee transferredEmployee =
            CreateEmployee(
                seed.Employee.Id,
                seed.Employee.EmployeeCode,
                seed.Employee.HireDate,
                seed.TargetDepartment,
                seed.TargetPosition);

        // Cố tình cho assignment mới thuộc
        // EmploymentPeriod khác.
        var invalidNewAssignment =
            new EmployeeOrganizationAssignment(
                Guid.NewGuid(),
                seed.Employee.Id,
                Guid.NewGuid(),
                seed.TargetDepartment.Id,
                seed.TargetDepartment.Code,
                seed.TargetDepartment.Name,
                seed.TargetPosition.Id,
                seed.TargetPosition.Code,
                seed.TargetPosition.Name,
                effectiveDate);

        var persistence =
            new EfEmployeeOrganizationTransferPersistence(
                new TestDbContextFactory(
                    options));

        ArgumentException exception =
            await Assert.ThrowsAsync<ArgumentException>(
                () =>
                    persistence
                        .TransferEmployeeOrganizationAsync(
                            transferredEmployee,
                            seed.CurrentAssignment,
                            invalidNewAssignment));

        Assert.Equal(
            "Điều chuyển phải nằm trong cùng "
            + "giai đoạn làm việc.",
            exception.Message);

        // Validation xảy ra trước SaveChanges,
        // database phải nguyên vẹn.
        await using var verificationContext =
            new HrManagementDbContext(
                options);

        Employee persistedEmployee =
            await verificationContext
                .Employees
                .SingleAsync();

        EmployeeOrganizationAssignment persistedAssignment =
            await verificationContext
                .EmployeeOrganizationAssignments
                .SingleAsync();

        Assert.Equal(
            seed.SourceDepartment.Id,
            persistedEmployee.DepartmentId);

        Assert.Equal(
            seed.SourcePosition.Id,
            persistedEmployee.PositionId);

        Assert.True(
            persistedAssignment.IsOpen);

        Assert.Null(
            persistedAssignment.EndDate);
    }

    private static async Task<SeedData> SeedAsync(
        DbContextOptions<HrManagementDbContext> options)
    {
        var sourceDepartment =
            new Department(
                Guid.NewGuid(),
                "DEV",
                "Phát triển phần mềm");

        var targetDepartment =
            new Department(
                Guid.NewGuid(),
                "RD",
                "Nghiên cứu và phát triển");

        var sourcePosition =
            new Position(
                Guid.NewGuid(),
                "SWE",
                "Kỹ sư phần mềm");

        var targetPosition =
            new Position(
                Guid.NewGuid(),
                "LEAD",
                "Trưởng nhóm kỹ thuật");

        DateOnly hireDate =
            new(2025, 1, 10);

        Employee employee =
            CreateEmployee(
                Guid.NewGuid(),
                "EMP-TRANSFER-001",
                hireDate,
                sourceDepartment,
                sourcePosition);

        var employmentPeriod =
            new EmploymentPeriod(
                Guid.NewGuid(),
                employee.Id,
                hireDate);

        var currentAssignment =
            new EmployeeOrganizationAssignment(
                Guid.NewGuid(),
                employee.Id,
                employmentPeriod.Id,
                sourceDepartment.Id,
                sourceDepartment.Code,
                sourceDepartment.Name,
                sourcePosition.Id,
                sourcePosition.Code,
                sourcePosition.Name,
                hireDate);

        await using var dbContext =
            new HrManagementDbContext(
                options);

        await dbContext.Database
            .EnsureCreatedAsync();

        dbContext.Departments.AddRange(
            sourceDepartment,
            targetDepartment);

        dbContext.Positions.AddRange(
            sourcePosition,
            targetPosition);

        dbContext.Employees.Add(
            employee);

        dbContext.EmploymentPeriods.Add(
            employmentPeriod);

        dbContext.EmployeeOrganizationAssignments.Add(
            currentAssignment);

        await dbContext.SaveChangesAsync();

        return new SeedData(
            employee,
            employmentPeriod,
            currentAssignment,
            sourceDepartment,
            targetDepartment,
            sourcePosition,
            targetPosition);
    }

    private static Employee CreateEmployee(
        Guid employeeId,
        string employeeCode,
        DateOnly hireDate,
        Department department,
        Position position)
    {
        return new Employee(
            employeeId,
            employeeCode,
            $"Nhân viên {employeeCode}",
            "transfer@example.com",
            "0901000000",
            new DateOnly(1995, 1, 1),
            hireDate,
            department.Name,
            position.Name,
            EmployeeStatus.Active,
            departmentId: department.Id,
            positionId: position.Id);
    }

    private static async Task<SqliteConnection>
        CreateOpenConnectionAsync()
    {
        var connection =
            new SqliteConnection(
                "Data Source=:memory:;Foreign Keys=True");

        await connection.OpenAsync();

        return connection;
    }

    private static DbContextOptions<HrManagementDbContext>
        CreateOptions(
            SqliteConnection connection)
    {
        return new DbContextOptionsBuilder<HrManagementDbContext>()
            .UseSqlite(
                connection)
            .Options;
    }

    private sealed record SeedData(
        Employee Employee,
        EmploymentPeriod EmploymentPeriod,
        EmployeeOrganizationAssignment CurrentAssignment,
        Department SourceDepartment,
        Department TargetDepartment,
        Position SourcePosition,
        Position TargetPosition);

    private sealed class TestDbContextFactory
        : IDbContextFactory<HrManagementDbContext>
    {
        private readonly
            DbContextOptions<HrManagementDbContext>
                _options;

        public TestDbContextFactory(
            DbContextOptions<HrManagementDbContext> options)
        {
            _options =
                options;
        }

        public HrManagementDbContext CreateDbContext()
        {
            return new HrManagementDbContext(
                _options);
        }

        public Task<HrManagementDbContext>
            CreateDbContextAsync(
                CancellationToken cancellationToken = default)
        {
            return Task.FromResult(
                CreateDbContext());
        }
    }
}
