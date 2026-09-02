using HrManagement.Domain.Attendance.Timesheets;
using HrManagement.Domain.Employees;
using HrManagement.Domain.Payroll.Periods;
using HrManagement.Domain.Payroll.Snapshots;
using HrManagement.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace HrManagement.Tests.Payroll;

public sealed class PayrollEmployeeSnapshotPersistenceTests
{
    [Fact]
    public async Task SaveAsync_PersistsPayrollEmployeeSnapshot()
    {
        await using TestDatabase database =
            await TestDatabase.CreateAsync();

        SeedResult seed =
            await database.SeedAsync();

        var snapshot =
            CreateSnapshot(
                seed.PayrollPeriodId,
                seed.EmployeeId);

        await using (
            HrManagementDbContext dbContext =
                database.CreateContext())
        {
            dbContext
                .PayrollEmployeeSnapshots
                .Add(
                    snapshot);

            await dbContext.SaveChangesAsync();
        }

        await using HrManagementDbContext verification =
            database.CreateContext();

        PayrollEmployeeSnapshot saved =
            await verification
                .PayrollEmployeeSnapshots
                .AsNoTracking()
                .SingleAsync();

        Assert.Equal(
            seed.PayrollPeriodId,
            saved.PayrollPeriodId);

        Assert.Equal(
            seed.EmployeeId,
            saved.EmployeeId);

        Assert.Equal(
            "EMP001",
            saved.EmployeeCode);

        Assert.Equal(
            "Nguyễn Văn An",
            saved.EmployeeFullName);

        Assert.Equal(
            "VND",
            saved.CurrencyCode);

        Assert.Equal(
            25_000_000m,
            saved.BaseSalaryAmount);

        Assert.Equal(
            120,
            saved.ApprovedOvertimeMinutes);

        Assert.Equal(
            90,
            saved.PayableOvertimeMinutes);

        Assert.Equal(
            500_000m,
            saved.OvertimeAmount);

        Assert.Equal(
            25_500_000m,
            saved.GrossAmount);
    }

    [Fact]
    public async Task SaveAsync_WhenSameEmployeeIsSnapshottedTwiceInPeriod_IsRejected()
    {
        await using TestDatabase database =
            await TestDatabase.CreateAsync();

        SeedResult seed =
            await database.SeedAsync();

        await using HrManagementDbContext dbContext =
            database.CreateContext();

        dbContext
            .PayrollEmployeeSnapshots
            .AddRange(
                CreateSnapshot(
                    seed.PayrollPeriodId,
                    seed.EmployeeId),
                CreateSnapshot(
                    seed.PayrollPeriodId,
                    seed.EmployeeId));

        await Assert.ThrowsAsync<DbUpdateException>(
            () =>
                dbContext.SaveChangesAsync());
    }

    [Fact]
    public async Task SaveAsync_WhenPayrollPeriodDoesNotExist_IsRejected()
    {
        await using TestDatabase database =
            await TestDatabase.CreateAsync();

        SeedResult seed =
            await database.SeedAsync();

        await using HrManagementDbContext dbContext =
            database.CreateContext();

        dbContext
            .PayrollEmployeeSnapshots
            .Add(
                CreateSnapshot(
                    Guid.NewGuid(),
                    seed.EmployeeId));

        await Assert.ThrowsAsync<DbUpdateException>(
            () =>
                dbContext.SaveChangesAsync());
    }

    [Fact]
    public async Task SaveAsync_WhenEmployeeDoesNotExist_IsRejected()
    {
        await using TestDatabase database =
            await TestDatabase.CreateAsync();

        SeedResult seed =
            await database.SeedAsync();

        await using HrManagementDbContext dbContext =
            database.CreateContext();

        dbContext
            .PayrollEmployeeSnapshots
            .Add(
                CreateSnapshot(
                    seed.PayrollPeriodId,
                    Guid.NewGuid()));

        await Assert.ThrowsAsync<DbUpdateException>(
            () =>
                dbContext.SaveChangesAsync());
    }

    private static PayrollEmployeeSnapshot CreateSnapshot(
        Guid payrollPeriodId,
        Guid employeeId)
    {
        return new PayrollEmployeeSnapshot(
            Guid.NewGuid(),
            payrollPeriodId,
            employeeId,
            "EMP001",
            "Nguyễn Văn An",
            "VND",
            25_000_000m,
            120,
            90,
            500_000m,
            25_500_000m);
    }

    private sealed record SeedResult(
        Guid EmployeeId,
        Guid PayrollPeriodId);

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

        public async Task<SeedResult> SeedAsync()
        {
            Guid employeeId =
                Guid.NewGuid();

            Guid timesheetPeriodId =
                Guid.NewGuid();

            Guid payrollPeriodId =
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

            var timesheetPeriod =
                new TimesheetPeriod(
                    timesheetPeriodId,
                    2026,
                    8);

            timesheetPeriod.Close(
                Utc(
                    9),
                "user-1",
                "admin");

            var payrollPeriod =
                new PayrollPeriod(
                    payrollPeriodId,
                    timesheetPeriodId,
                    2026,
                    8);

            await using HrManagementDbContext dbContext =
                CreateContext();

            dbContext.Employees.Add(
                employee);

            dbContext.TimesheetPeriods.Add(
                timesheetPeriod);

            dbContext.PayrollPeriods.Add(
                payrollPeriod);

            await dbContext.SaveChangesAsync();

            return new SeedResult(
                employeeId,
                payrollPeriodId);
        }

        public async ValueTask DisposeAsync()
        {
            await _connection.DisposeAsync();
        }
    }

    private static DateTime Utc(
        int hour)
    {
        return new DateTime(
            2026,
            8,
            31,
            hour,
            0,
            0,
            DateTimeKind.Utc);
    }
}
