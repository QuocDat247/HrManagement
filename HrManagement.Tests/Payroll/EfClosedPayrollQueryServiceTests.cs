using HrManagement.Application.Payroll.Periods;
using HrManagement.Domain.Attendance.Timesheets;
using HrManagement.Domain.Employees;
using HrManagement.Domain.Payroll.Periods;
using HrManagement.Domain.Payroll.Snapshots;
using HrManagement.Infrastructure.Payroll.Periods;
using HrManagement.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace HrManagement.Tests.Payroll;

public sealed class EfClosedPayrollQueryServiceTests
{
    [Fact]
    public async Task GetAsync_WhenPeriodDoesNotExist_ReturnsNull()
    {
        await using TestDatabase database =
            await TestDatabase.CreateAsync();

        ClosedPayrollReadModel? result =
            await database.QueryService
                .GetAsync(
                    2026,
                    8);

        Assert.Null(
            result);
    }

    [Fact]
    public async Task GetAsync_WhenClosedPayrollExists_ReturnsImmutableSnapshotData()
    {
        await using TestDatabase database =
            await TestDatabase.CreateAsync();

        SeedResult seed =
            await database.SeedClosedPayrollAsync();

        ClosedPayrollReadModel result =
            await database.QueryService
                .GetAsync(
                    2026,
                    8)
            ?? throw new InvalidOperationException();

        Assert.Equal(
            seed.PayrollPeriodId,
            result.PayrollPeriodId);

        Assert.Equal(
            seed.TimesheetPeriodId,
            result.TimesheetPeriodId);

        Assert.Equal(
            Utc(
                12),
            result.ClosedAtUtc);

        Assert.Equal(
            "user-1",
            result.ClosedByUserId);

        Assert.Equal(
            "admin",
            result.ClosedByUsername);

        Assert.Equal(
            2,
            result.SnapshotCount);

        ClosedPayrollEmployeeItem first =
            result.Employees[0];

        Assert.Equal(
            "EMP001",
            first.EmployeeCode);

        Assert.Equal(
            "Nguyễn Văn An",
            first.EmployeeFullName);

        Assert.Equal(
            "VND",
            first.CurrencyCode);

        Assert.Equal(
            25_000_000m,
            first.BaseSalaryAmount);

        Assert.Equal(
            120,
            first.ApprovedOvertimeMinutes);

        Assert.Equal(
            90,
            first.PayableOvertimeMinutes);

        Assert.Equal(
            500_000m,
            first.OvertimeAmount);

        Assert.Equal(
            25_500_000m,
            first.GrossAmount);
    }

    [Fact]
    public async Task GetAsync_GroupsTotalsByCurrencyWithoutMixingCurrencies()
    {
        await using TestDatabase database =
            await TestDatabase.CreateAsync();

        await database.SeedClosedPayrollAsync();

        ClosedPayrollReadModel result =
            await database.QueryService
                .GetAsync(
                    2026,
                    8)
            ?? throw new InvalidOperationException();

        Assert.Equal(
            2,
            result.CurrencySummaries.Count);

        ClosedPayrollCurrencySummary usd =
            result.CurrencySummaries
                .Single(
                    summary =>
                        summary.CurrencyCode ==
                        "USD");

        Assert.Equal(
            1,
            usd.EmployeeCount);

        Assert.Equal(
            1_000m,
            usd.BaseSalaryAmount);

        Assert.Equal(
            100m,
            usd.OvertimeAmount);

        Assert.Equal(
            1_100m,
            usd.GrossAmount);

        ClosedPayrollCurrencySummary vnd =
            result.CurrencySummaries
                .Single(
                    summary =>
                        summary.CurrencyCode ==
                        "VND");

        Assert.Equal(
            25_000_000m,
            vnd.BaseSalaryAmount);

        Assert.Equal(
            500_000m,
            vnd.OvertimeAmount);

        Assert.Equal(
            25_500_000m,
            vnd.GrossAmount);
    }

    [Fact]
    public async Task GetAsync_WhenPersistedPeriodIsOpen_RejectsInvalidClosedPayrollState()
    {
        await using TestDatabase database =
            await TestDatabase.CreateAsync();

        await database.SeedOpenPayrollPeriodAsync();

        InvalidOperationException exception =
            await Assert.ThrowsAsync<InvalidOperationException>(
                () =>
                    database.QueryService
                        .GetAsync(
                            2026,
                            8));

        Assert.Contains(
            "chưa có trạng thái đóng hợp lệ",
            exception.Message);
    }

    private sealed record SeedResult(
        Guid TimesheetPeriodId,
        Guid PayrollPeriodId);

    private sealed class TestDatabase
        : IAsyncDisposable
    {
        private readonly SqliteConnection
            _connection;

        private readonly DbContextOptions<HrManagementDbContext>
            _options;

        private readonly TestDbContextFactory
            _factory;

        public EfClosedPayrollQueryService QueryService
        {
            get;
        }

        private TestDatabase(
            SqliteConnection connection,
            DbContextOptions<HrManagementDbContext> options,
            TestDbContextFactory factory)
        {
            _connection =
                connection;

            _options =
                options;

            _factory =
                factory;

            QueryService =
                new EfClosedPayrollQueryService(
                    factory);
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

            var factory =
                new TestDbContextFactory(
                    options);

            await using (
                HrManagementDbContext dbContext =
                    await factory
                        .CreateDbContextAsync())
            {
                await dbContext.Database
                    .EnsureCreatedAsync();
            }

            return new TestDatabase(
                connection,
                options,
                factory);
        }

        public HrManagementDbContext CreateContext()
        {
            return new HrManagementDbContext(
                _options);
        }

        public async Task<SeedResult>
            SeedClosedPayrollAsync()
        {
            Guid employee1Id =
                Guid.NewGuid();

            Guid employee2Id =
                Guid.NewGuid();

            Guid timesheetPeriodId =
                Guid.NewGuid();

            Guid payrollPeriodId =
                Guid.NewGuid();

            var employee1 =
                CreateEmployee(
                    employee1Id,
                    "EMP001",
                    "Nguyễn Văn An");

            var employee2 =
                CreateEmployee(
                    employee2Id,
                    "EMP002",
                    "Trần Minh Bình");

            var timesheetPeriod =
                new TimesheetPeriod(
                    timesheetPeriodId,
                    2026,
                    8);

            timesheetPeriod.Close(
                Utc(
                    9),
                "timesheet-user",
                "timesheet-admin");

            var payrollPeriod =
                new PayrollPeriod(
                    payrollPeriodId,
                    timesheetPeriodId,
                    2026,
                    8);

            payrollPeriod.Close(
                Utc(
                    12),
                "user-1",
                "admin");

            var firstSnapshot =
                new PayrollEmployeeSnapshot(
                    Guid.NewGuid(),
                    payrollPeriodId,
                    employee1Id,
                    "EMP001",
                    "Nguyễn Văn An",
                    "VND",
                    25_000_000m,
                    120,
                    90,
                    500_000m,
                    25_500_000m);

            var secondSnapshot =
                new PayrollEmployeeSnapshot(
                    Guid.NewGuid(),
                    payrollPeriodId,
                    employee2Id,
                    "EMP002",
                    "Trần Minh Bình",
                    "USD",
                    1_000m,
                    60,
                    60,
                    100m,
                    1_100m);

            await using HrManagementDbContext dbContext =
                CreateContext();

            dbContext.Employees.AddRange(
                employee1,
                employee2);

            dbContext.TimesheetPeriods.Add(
                timesheetPeriod);

            dbContext.PayrollPeriods.Add(
                payrollPeriod);

            dbContext.PayrollEmployeeSnapshots.AddRange(
                firstSnapshot,
                secondSnapshot);

            await dbContext.SaveChangesAsync();

            return new SeedResult(
                timesheetPeriodId,
                payrollPeriodId);
        }

        public async Task SeedOpenPayrollPeriodAsync()
        {
            Guid timesheetPeriodId =
                Guid.NewGuid();

            var timesheetPeriod =
                new TimesheetPeriod(
                    timesheetPeriodId,
                    2026,
                    8);

            timesheetPeriod.Close(
                Utc(
                    9),
                "timesheet-user",
                "timesheet-admin");

            var payrollPeriod =
                new PayrollPeriod(
                    Guid.NewGuid(),
                    timesheetPeriodId,
                    2026,
                    8);

            await using HrManagementDbContext dbContext =
                CreateContext();

            dbContext.TimesheetPeriods.Add(
                timesheetPeriod);

            dbContext.PayrollPeriods.Add(
                payrollPeriod);

            await dbContext.SaveChangesAsync();
        }

        private static Employee CreateEmployee(
            Guid employeeId,
            string code,
            string fullName)
        {
            return new Employee(
                employeeId,
                code,
                fullName,
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
                    "Nhân viên",
                status:
                    EmployeeStatus.Active);
        }

        public async ValueTask DisposeAsync()
        {
            await _connection.DisposeAsync();
        }
    }

    private sealed class TestDbContextFactory
        : IDbContextFactory<HrManagementDbContext>
    {
        private readonly DbContextOptions<HrManagementDbContext>
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
