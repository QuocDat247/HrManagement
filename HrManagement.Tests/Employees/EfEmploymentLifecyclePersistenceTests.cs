using HrManagement.Application.Employees.EmploymentLifecycle;
using HrManagement.Domain.Employees;
using HrManagement.Infrastructure.Employees;
using HrManagement.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using static HrManagement.Tests.Employees.EfEmploymentHistoryRepositoryTests;

namespace HrManagement.Tests.Employees;
public sealed class EfEmploymentLifecyclePersistenceTests
{
    [Fact]
    public async Task CreateEmployeeWithPeriodAsync_PersistsBothEntities()
    {
        await using var connection =
            new SqliteConnection(
                "Data Source=:memory:");

        await connection.OpenAsync();

        DbContextOptions<HrManagementDbContext> options =
            new DbContextOptionsBuilder<HrManagementDbContext>()
                .UseSqlite(connection)
                .Options;

        await using (var dbContext =
                     new HrManagementDbContext(options))
        {
            await dbContext.Database.EnsureCreatedAsync();
        }

        Guid employeeId =
            Guid.NewGuid();

        DateOnly hireDate =
            new(2026, 8, 12);

        Employee employee =
            CreateEmployee(
                employeeId,
                "EMP-ATOMIC-001",
                hireDate,
                EmployeeStatus.Active);

        var period =
            new EmploymentPeriod(
                Guid.NewGuid(),
                employeeId,
                hireDate);

        var persistence =
            new EfEmploymentLifecyclePersistence(
                new TestDbContextFactory(options));

        await persistence.CreateEmployeeWithPeriodAsync(
            employee,
            period);

        await using var verificationContext =
            new HrManagementDbContext(options);

        Employee persistedEmployee =
            await verificationContext.Employees
                .SingleAsync();

        EmploymentPeriod persistedPeriod =
            await verificationContext
                .EmploymentPeriods
                .SingleAsync();

        Assert.Equal(
            employeeId,
            persistedEmployee.Id);

        Assert.Equal(
            employeeId,
            persistedPeriod.EmployeeId);

        Assert.Equal(
            hireDate,
            persistedPeriod.StartDate);

        Assert.Null(
            persistedPeriod.EndDate);
    }

    [Fact]
    public async Task UpdateEmployeeWithPeriodAsync_PersistsEmployeeAndClosedPeriod()
    {
        await using var connection =
            new SqliteConnection(
                "Data Source=:memory:");

        await connection.OpenAsync();

        DbContextOptions<HrManagementDbContext> options =
            new DbContextOptionsBuilder<HrManagementDbContext>()
                .UseSqlite(connection)
                .Options;

        Guid employeeId =
            Guid.NewGuid();

        Guid periodId =
            Guid.NewGuid();

        DateOnly hireDate =
            new(2025, 1, 10);

        await using (var dbContext =
                     new HrManagementDbContext(options))
        {
            await dbContext.Database.EnsureCreatedAsync();

            dbContext.Employees.Add(
                CreateEmployee(
                    employeeId,
                    "EMP-ATOMIC-002",
                    hireDate,
                    EmployeeStatus.Active));

            dbContext.EmploymentPeriods.Add(
                new EmploymentPeriod(
                    periodId,
                    employeeId,
                    hireDate));

            await dbContext.SaveChangesAsync();
        }

        DateOnly terminationDate =
            new(2026, 8, 12);

        Employee inactiveEmployee =
            CreateEmployee(
                employeeId,
                "EMP-ATOMIC-002",
                hireDate,
                EmployeeStatus.Inactive,
                terminationDate);

        var closedPeriod =
            new EmploymentPeriod(
                periodId,
                employeeId,
                hireDate);

        closedPeriod.Close(
            terminationDate);

        var persistence =
            new EfEmploymentLifecyclePersistence(
                new TestDbContextFactory(options));

        await persistence.UpdateEmployeeWithPeriodAsync(
            inactiveEmployee,
            closedPeriod);

        await using var verificationContext =
            new HrManagementDbContext(options);

        Employee persistedEmployee =
            await verificationContext.Employees
                .SingleAsync();

        EmploymentPeriod persistedPeriod =
            await verificationContext
                .EmploymentPeriods
                .SingleAsync();

        Assert.Equal(
            EmployeeStatus.Inactive,
            persistedEmployee.Status);

        Assert.Equal(
            terminationDate,
            persistedEmployee.TerminationDate);

        Assert.Equal(
            terminationDate,
            persistedPeriod.EndDate);

        Assert.False(
            persistedPeriod.IsOpen);
    }

    [Fact]
    public async Task UpdateEmployeeWithPeriodAsync_WhenSaveFails_RollsBackBothChanges()
    {
        await using var connection =
            new SqliteConnection(
                "Data Source=:memory:");

        await connection.OpenAsync();

        DbContextOptions<HrManagementDbContext> options =
            new DbContextOptionsBuilder<HrManagementDbContext>()
                .UseSqlite(connection)
                .Options;

        Guid targetEmployeeId =
            Guid.NewGuid();

        Guid otherEmployeeId =
            Guid.NewGuid();

        Guid periodId =
            Guid.NewGuid();

        DateOnly hireDate =
            new(2025, 1, 10);

        await using (var dbContext =
                     new HrManagementDbContext(options))
        {
            await dbContext.Database.EnsureCreatedAsync();

            dbContext.Employees.AddRange(
                CreateEmployee(
                    targetEmployeeId,
                    "EMP-ATOMIC-A",
                    hireDate,
                    EmployeeStatus.Active),

                CreateEmployee(
                    otherEmployeeId,
                    "EMP-ATOMIC-B",
                    hireDate,
                    EmployeeStatus.Active));

            dbContext.EmploymentPeriods.Add(
                new EmploymentPeriod(
                    periodId,
                    targetEmployeeId,
                    hireDate));

            await dbContext.SaveChangesAsync();
        }

        DateOnly terminationDate =
            new(2026, 8, 12);

        // Cố tình dùng code của employee khác
        // để vi phạm unique EmployeeCode.
        Employee invalidUpdate =
            CreateEmployee(
                targetEmployeeId,
                "EMP-ATOMIC-B",
                hireDate,
                EmployeeStatus.Inactive,
                terminationDate);

        var closedPeriod =
            new EmploymentPeriod(
                periodId,
                targetEmployeeId,
                hireDate);

        closedPeriod.Close(
            terminationDate);

        var persistence =
            new EfEmploymentLifecyclePersistence(
                new TestDbContextFactory(options));

        await Assert.ThrowsAsync<DbUpdateException>(
            () => persistence.UpdateEmployeeWithPeriodAsync(
                invalidUpdate,
                closedPeriod));

        await using var verificationContext =
            new HrManagementDbContext(options);

        Employee originalEmployee =
            await verificationContext.Employees
                .SingleAsync(employee =>
                    employee.Id == targetEmployeeId);

        EmploymentPeriod originalPeriod =
            await verificationContext
                .EmploymentPeriods
                .SingleAsync(period =>
                    period.Id == periodId);

        Assert.Equal(
            "EMP-ATOMIC-A",
            originalEmployee.EmployeeCode);

        Assert.Equal(
            EmployeeStatus.Active,
            originalEmployee.Status);

        Assert.Null(
            originalEmployee.TerminationDate);

        Assert.Null(
            originalPeriod.EndDate);

        Assert.True(
            originalPeriod.IsOpen);
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

    [Fact]
    public async Task UpdateEmployeeWithNewPeriodAsync_PersistsRestoredEmployeeAndNewOpenPeriod()
    {
        await using var connection =
            new SqliteConnection(
                "Data Source=:memory:");

        await connection.OpenAsync();

        DbContextOptions<HrManagementDbContext> options =
            new DbContextOptionsBuilder<HrManagementDbContext>()
                .UseSqlite(connection)
                .Options;

        Guid employeeId =
            Guid.NewGuid();

        Guid previousPeriodId =
            Guid.NewGuid();

        DateOnly originalHireDate =
            new(2022, 1, 10);

        DateOnly terminationDate =
            new(2026, 3, 15);

        await using (var dbContext =
                     new HrManagementDbContext(options))
        {
            await dbContext.Database
                .EnsureCreatedAsync();

            dbContext.Employees.Add(
                CreateEmployee(
                    employeeId,
                    "EMP-REHIRE-001",
                    originalHireDate,
                    EmployeeStatus.Inactive,
                    terminationDate));

            dbContext.EmploymentPeriods.Add(
                new EmploymentPeriod(
                    previousPeriodId,
                    employeeId,
                    originalHireDate,
                    terminationDate));

            await dbContext.SaveChangesAsync();
        }

        DateOnly rehireDate =
            new(2026, 8, 12);

        Employee restoredEmployee =
            CreateEmployee(
                employeeId,
                "EMP-REHIRE-001",
                originalHireDate,
                EmployeeStatus.Active);

        var newPeriod =
            new EmploymentPeriod(
                Guid.NewGuid(),
                employeeId,
                rehireDate);

        var persistence =
            new EfEmploymentLifecyclePersistence(
                new TestDbContextFactory(options));

        await persistence
            .UpdateEmployeeWithNewPeriodAsync(
                restoredEmployee,
                newPeriod);

        await using var verificationContext =
            new HrManagementDbContext(options);

        Employee persistedEmployee =
            await verificationContext.Employees
                .SingleAsync();

        List<EmploymentPeriod> periods =
            await verificationContext
                .EmploymentPeriods
                .OrderBy(period =>
                    period.StartDate)
                .ToListAsync();

        Assert.Equal(
            EmployeeStatus.Active,
            persistedEmployee.Status);

        Assert.Null(
            persistedEmployee.TerminationDate);

        // Ngày tuyển ban đầu của Employee không bị thay.
        Assert.Equal(
            originalHireDate,
            persistedEmployee.HireDate);

        Assert.Equal(
            2,
            periods.Count);

        EmploymentPeriod previousPeriod =
            periods[0];

        EmploymentPeriod rehiredPeriod =
            periods[1];

        Assert.Equal(
            previousPeriodId,
            previousPeriod.Id);

        Assert.Equal(
            terminationDate,
            previousPeriod.EndDate);

        Assert.Equal(
            rehireDate,
            rehiredPeriod.StartDate);

        Assert.Null(
            rehiredPeriod.EndDate);

        Assert.True(
            rehiredPeriod.IsOpen);
    }

    [Fact]
    public async Task UpdateEmployeeWithNewPeriodAsync_WhenNewPeriodCannotBeInserted_RollsBackEmployeeUpdate()
    {
        await using var connection =
            new SqliteConnection(
                "Data Source=:memory:");

        await connection.OpenAsync();

        DbContextOptions<HrManagementDbContext> options =
            new DbContextOptionsBuilder<HrManagementDbContext>()
                .UseSqlite(connection)
                .Options;

        Guid employeeId =
            Guid.NewGuid();

        Guid existingPeriodId =
            Guid.NewGuid();

        DateOnly hireDate =
            new(2022, 1, 10);

        DateOnly terminationDate =
            new(2026, 3, 15);

        await using (var dbContext =
                     new HrManagementDbContext(options))
        {
            await dbContext.Database
                .EnsureCreatedAsync();

            dbContext.Employees.Add(
                CreateEmployee(
                    employeeId,
                    "EMP-REHIRE-002",
                    hireDate,
                    EmployeeStatus.Inactive,
                    terminationDate));

            dbContext.EmploymentPeriods.Add(
                new EmploymentPeriod(
                    existingPeriodId,
                    employeeId,
                    hireDate,
                    terminationDate));

            await dbContext.SaveChangesAsync();
        }

        Employee restoredEmployee =
            CreateEmployee(
                employeeId,
                "EMP-REHIRE-002",
                hireDate,
                EmployeeStatus.Active);

        // Cố tình trùng PK với period cũ.
        var invalidNewPeriod =
            new EmploymentPeriod(
                existingPeriodId,
                employeeId,
                new DateOnly(2026, 8, 12));

        var persistence =
            new EfEmploymentLifecyclePersistence(
                new TestDbContextFactory(options));

        await Assert.ThrowsAsync<DbUpdateException>(
            () => persistence
                .UpdateEmployeeWithNewPeriodAsync(
                    restoredEmployee,
                    invalidNewPeriod));

        await using var verificationContext =
            new HrManagementDbContext(options);

        Employee persistedEmployee =
            await verificationContext.Employees
                .SingleAsync();

        List<EmploymentPeriod> periods =
            await verificationContext
                .EmploymentPeriods
                .ToListAsync();

        Assert.Equal(
            EmployeeStatus.Inactive,
            persistedEmployee.Status);

        Assert.Equal(
            terminationDate,
            persistedEmployee.TerminationDate);

        Assert.Single(
            periods);

        Assert.Equal(
            existingPeriodId,
            periods[0].Id);

        Assert.Equal(
            terminationDate,
            periods[0].EndDate);
    }
}
