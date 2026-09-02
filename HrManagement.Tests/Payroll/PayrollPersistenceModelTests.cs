using HrManagement.Domain.Attendance.Timesheets;
using HrManagement.Domain.Employees;
using HrManagement.Domain.Payroll.Compensation;
using HrManagement.Domain.Payroll.Periods;
using HrManagement.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace HrManagement.Tests.Payroll;

public sealed class PayrollPersistenceModelTests
{
    [Fact]
    public async Task SaveAsync_PersistsCompensationAndPayrollPeriod()
    {
        await using TestDatabase database =
            await TestDatabase.CreateAsync();

        SeedResult seed =
            await database.SeedDependenciesAsync();

        var compensation =
            new EmployeeCompensation(
                Guid.NewGuid(),
                seed.EmployeeId,
                seed.EmploymentPeriodId,
                new DateOnly(
                    2026,
                    8,
                    1),
                25_000_000m,
                "VND");

        var payrollPeriod =
            new PayrollPeriod(
                Guid.NewGuid(),
                seed.TimesheetPeriodId,
                2026,
                8);

        await using (
            HrManagementDbContext dbContext =
                database.CreateContext())
        {
            dbContext.EmployeeCompensations.Add(
                compensation);

            dbContext.PayrollPeriods.Add(
                payrollPeriod);

            await dbContext.SaveChangesAsync();
        }

        await using HrManagementDbContext verification =
            database.CreateContext();

        EmployeeCompensation savedCompensation =
            await verification
                .EmployeeCompensations
                .AsNoTracking()
                .SingleAsync();

        Assert.Equal(
            25_000_000m,
            savedCompensation.MonthlyBaseSalary);

        Assert.Equal(
            "VND",
            savedCompensation.CurrencyCode);

        Assert.True(
            savedCompensation.IsOpen);

        PayrollPeriod savedPeriod =
            await verification
                .PayrollPeriods
                .AsNoTracking()
                .SingleAsync();

        Assert.Equal(
            seed.TimesheetPeriodId,
            savedPeriod.TimesheetPeriodId);

        Assert.Equal(
            PayrollPeriodStatus.Open,
            savedPeriod.Status);
    }

    [Fact]
    public async Task SaveAsync_WhenSecondOpenCompensationExistsForSameEmploymentPeriod_IsRejected()
    {
        await using TestDatabase database =
            await TestDatabase.CreateAsync();

        SeedResult seed =
            await database.SeedDependenciesAsync();

        await using HrManagementDbContext dbContext =
            database.CreateContext();

        dbContext.EmployeeCompensations.Add(
            new EmployeeCompensation(
                Guid.NewGuid(),
                seed.EmployeeId,
                seed.EmploymentPeriodId,
                new DateOnly(
                    2026,
                    8,
                    1),
                25_000_000m,
                "VND"));

        await dbContext.SaveChangesAsync();

        dbContext.EmployeeCompensations.Add(
            new EmployeeCompensation(
                Guid.NewGuid(),
                seed.EmployeeId,
                seed.EmploymentPeriodId,
                new DateOnly(
                    2026,
                    9,
                    1),
                28_000_000m,
                "VND"));

        await Assert.ThrowsAsync<DbUpdateException>(
            () =>
                dbContext.SaveChangesAsync());
    }

    [Fact]
    public async Task SaveAsync_AfterClosingCompensation_AllowsNewOpenCompensation()
    {
        await using TestDatabase database =
            await TestDatabase.CreateAsync();

        SeedResult seed =
            await database.SeedDependenciesAsync();

        var first =
            new EmployeeCompensation(
                Guid.NewGuid(),
                seed.EmployeeId,
                seed.EmploymentPeriodId,
                new DateOnly(
                    2026,
                    8,
                    1),
                25_000_000m,
                "VND");

        first.Close(
            new DateOnly(
                2026,
                8,
                31));

        var replacement =
            new EmployeeCompensation(
                Guid.NewGuid(),
                seed.EmployeeId,
                seed.EmploymentPeriodId,
                new DateOnly(
                    2026,
                    9,
                    1),
                28_000_000m,
                "VND");

        await using (
            HrManagementDbContext dbContext =
                database.CreateContext())
        {
            dbContext.EmployeeCompensations.AddRange(
                first,
                replacement);

            await dbContext.SaveChangesAsync();
        }

        await using HrManagementDbContext verification =
            database.CreateContext();

        EmployeeCompensation[] rows =
            await verification
                .EmployeeCompensations
                .AsNoTracking()
                .OrderBy(
                    compensation =>
                        compensation.EffectiveFrom)
                .ToArrayAsync();

        Assert.Equal(
            2,
            rows.Length);

        Assert.Equal(
            new DateOnly(
                2026,
                8,
                31),
            rows[0].EffectiveTo);

        Assert.True(
            rows[1].IsOpen);

        Assert.Equal(
            28_000_000m,
            rows[1].MonthlyBaseSalary);
    }

    [Fact]
    public async Task SaveAsync_WhenTimesheetSourceIsReusedByAnotherPayrollPeriod_IsRejected()
    {
        await using TestDatabase database =
            await TestDatabase.CreateAsync();

        SeedResult seed =
            await database.SeedDependenciesAsync();

        await using HrManagementDbContext dbContext =
            database.CreateContext();

        dbContext.PayrollPeriods.Add(
            new PayrollPeriod(
                Guid.NewGuid(),
                seed.TimesheetPeriodId,
                2026,
                8));

        await dbContext.SaveChangesAsync();

        dbContext.PayrollPeriods.Add(
            new PayrollPeriod(
                Guid.NewGuid(),
                seed.TimesheetPeriodId,
                2026,
                9));

        await Assert.ThrowsAsync<DbUpdateException>(
            () =>
                dbContext.SaveChangesAsync());
    }

    [Fact]
    public async Task Model_DeclaresUniquePayrollPeriodByYearAndMonth()
    {
        await using TestDatabase database =
            await TestDatabase.CreateAsync();

        await using HrManagementDbContext dbContext =
            database.CreateContext();

        IEntityType entityType =
            dbContext.Model.FindEntityType(
                typeof(PayrollPeriod))
            ?? throw new InvalidOperationException(
                "Không tìm thấy model PayrollPeriod.");

        IIndex index =
            entityType
                .GetIndexes()
                .Single(
                    candidate =>
                        candidate.Properties
                            .Select(
                                property =>
                                    property.Name)
                            .SequenceEqual(
                            [
                                nameof(PayrollPeriod.Year),
                                nameof(PayrollPeriod.Month)
                            ]));

        Assert.True(
            index.IsUnique);

        Assert.Equal(
            "UX_PayrollPeriods_Year_Month",
            index.GetDatabaseName());
    }

    private sealed record SeedResult(
        Guid EmployeeId,
        Guid EmploymentPeriodId,
        Guid TimesheetPeriodId);

    private sealed class TestDatabase
        : IAsyncDisposable
    {
        private readonly SqliteConnection
            _connection;

        private readonly DbContextOptions<HrManagementDbContext>
            _options;

        private TestDatabase(
            SqliteConnection connection,
            DbContextOptions<HrManagementDbContext> options)
        {
            _connection =
                connection;

            _options =
                options;
        }

        public static async Task<TestDatabase> CreateAsync()
        {
            var connection =
                new SqliteConnection(
                    "Data Source=:memory:;Foreign Keys=True");

            await connection.OpenAsync();

            DbContextOptions<HrManagementDbContext> options =
                new DbContextOptionsBuilder<HrManagementDbContext>()
                    .UseSqlite(
                        connection)
                    .Options;

            var database =
                new TestDatabase(
                    connection,
                    options);

            await using (
                HrManagementDbContext dbContext =
                    database.CreateContext())
            {
                await dbContext.Database
                    .EnsureCreatedAsync();
            }

            return database;
        }

        public HrManagementDbContext CreateContext()
        {
            return new HrManagementDbContext(
                _options);
        }

        public async Task<SeedResult> SeedDependenciesAsync()
        {
            Guid employeeId =
                Guid.NewGuid();

            Guid employmentPeriodId =
                Guid.NewGuid();

            Guid timesheetPeriodId =
                Guid.NewGuid();

            var employee =
                new Employee(
                    employeeId,
                    "EMP001",
                    "Nguyễn Văn An",
                    email:
                        null,
                    phoneNumber:
                        null,
                    dateOfBirth:
                        null,
                    hireDate:
                        new DateOnly(
                            2026,
                            1,
                            1),
                    department:
                        "Phát triển",
                    position:
                        "Lập trình viên",
                    status:
                        EmployeeStatus.Active);

            var employmentPeriod =
                new EmploymentPeriod(
                    employmentPeriodId,
                    employeeId,
                    new DateOnly(
                        2026,
                        1,
                        1));

            var timesheetPeriod =
                new TimesheetPeriod(
                    timesheetPeriodId,
                    2026,
                    8);

            await using HrManagementDbContext dbContext =
                CreateContext();

            dbContext.Employees.Add(
                employee);

            dbContext.EmploymentPeriods.Add(
                employmentPeriod);

            dbContext.TimesheetPeriods.Add(
                timesheetPeriod);

            await dbContext.SaveChangesAsync();

            return new SeedResult(
                employeeId,
                employmentPeriodId,
                timesheetPeriodId);
        }

        public async ValueTask DisposeAsync()
        {
            await _connection.DisposeAsync();
        }
    }
}
