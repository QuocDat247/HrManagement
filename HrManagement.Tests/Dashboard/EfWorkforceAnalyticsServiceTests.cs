using HrManagement.Application.Dashboard.Analytics;
using HrManagement.Domain.Employees;
using HrManagement.Infrastructure.Dashboard.Analytics;
using HrManagement.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace HrManagement.Tests.Dashboard;

public sealed class EfWorkforceAnalyticsServiceTests
{
    [Fact]
    public async Task GetWorkforceMovementAsync_Monthly_ReturnsTwelvePeriodsAndCorrectMetrics()
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
            await dbContext.Database
                .EnsureCreatedAsync();

            Employee emp001 = CreateEmployee(
                "EMP001",
                new DateOnly(2025, 12, 15),
                EmployeeStatus.Active);
            Employee emp002 = CreateEmployee(
                "EMP002",
                new DateOnly(2026, 1, 10),
                EmployeeStatus.Active);
            Employee emp003 = CreateEmployee(
                "EMP003",
                new DateOnly(2026, 1, 20),
                EmployeeStatus.Inactive,
                new DateOnly(2026, 3, 5));
            Employee emp004 = CreateEmployee(
                "EMP004",
                new DateOnly(2026, 4, 1),
                EmployeeStatus.Active);
            Employee emp005 = CreateEmployee(
                "EMP005",
                new DateOnly(2025, 6, 1),
                EmployeeStatus.Inactive,
                new DateOnly(2026, 4, 15));
            // Legacy: biết đã nghỉ nhưng không biết ngày.
            Employee emp006 = CreateEmployee(
                "EMP006",
                new DateOnly(2024, 1, 1),
                EmployeeStatus.Inactive);

            dbContext.Employees.AddRange(
                emp001, emp002, emp003, emp004, emp005, emp006);

            // Seed period cho các employee có lịch sử xác định
            dbContext.EmploymentPeriods.AddRange(
                new EmploymentPeriod(
                    Guid.NewGuid(),
                    emp001.Id,
                    emp001.HireDate),
                new EmploymentPeriod(
                    Guid.NewGuid(),
                    emp002.Id,
                    emp002.HireDate),
                new EmploymentPeriod(
                    Guid.NewGuid(),
                    emp003.Id,
                    emp003.HireDate,
                    emp003.TerminationDate),
                new EmploymentPeriod(
                    Guid.NewGuid(),
                    emp004.Id,
                    emp004.HireDate),
                new EmploymentPeriod(
                    Guid.NewGuid(),
                    emp005.Id,
                    emp005.HireDate,
                    emp005.TerminationDate));
            // CỐ Ý không tạo period cho emp006 (legacy)

            await dbContext.SaveChangesAsync();
        }

        var service =
            new EfWorkforceAnalyticsService(
                new TestDbContextFactory(options));
        WorkforceMovementSummary summary =
            await service.GetWorkforceMovementAsync(
                2026,
                WorkforceAnalyticsGrouping.Monthly);

        Assert.Equal(12, summary.Periods.Count);
        Assert.Equal(2, summary.BeginningHeadcount);
        Assert.Equal(3, summary.EndingHeadcount);
        Assert.Equal(3, summary.TotalNewHires);
        Assert.Equal(2, summary.TotalSeparations);
        Assert.Equal(1, summary.NetChange);
        Assert.Equal(2.5m, summary.AverageHeadcount);
        Assert.Equal(80m, summary.TurnoverRate);
        Assert.Equal(
            1,
            summary.EmployeesWithUnknownTerminationDate);

        WorkforceMovementPeriod january =
            summary.Periods[0];
        Assert.Equal(2, january.NewHires);
        Assert.Equal(0, january.Separations);
        Assert.Equal(2, january.BeginningHeadcount);
        Assert.Equal(4, january.EndingHeadcount);
        Assert.Equal(3m, january.AverageHeadcount);
        Assert.Equal(0m, january.TurnoverRate);
        Assert.Equal(2, january.NetChange);

        WorkforceMovementPeriod march =
            summary.Periods[2];
        Assert.Equal(0, march.NewHires);
        Assert.Equal(1, march.Separations);
        Assert.Equal(4, march.BeginningHeadcount);
        Assert.Equal(3, march.EndingHeadcount);
        Assert.Equal(3.5m, march.AverageHeadcount);
        Assert.Equal(28.57m, march.TurnoverRate);
        Assert.Equal(-1, march.NetChange);

        WorkforceMovementPeriod april =
            summary.Periods[3];
        Assert.Equal(1, april.NewHires);
        Assert.Equal(1, april.Separations);
        Assert.Equal(3, april.BeginningHeadcount);
        Assert.Equal(3, april.EndingHeadcount);
        Assert.Equal(3m, april.AverageHeadcount);
        Assert.Equal(33.33m, april.TurnoverRate);
        Assert.Equal(0, april.NetChange);
    }

    [Fact]
    public async Task GetWorkforceMovementAsync_Quarterly_ReturnsFourPeriodsAndCorrectMetrics()
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
            await dbContext.Database
                .EnsureCreatedAsync();

            Employee emp001 = CreateEmployee(
                "EMP001",
                new DateOnly(2025, 12, 15),
                EmployeeStatus.Active);
            Employee emp002 = CreateEmployee(
                "EMP002",
                new DateOnly(2026, 1, 10),
                EmployeeStatus.Active);
            Employee emp003 = CreateEmployee(
                "EMP003",
                new DateOnly(2026, 1, 20),
                EmployeeStatus.Inactive,
                new DateOnly(2026, 3, 5));
            Employee emp004 = CreateEmployee(
                "EMP004",
                new DateOnly(2026, 4, 1),
                EmployeeStatus.Active);
            Employee emp005 = CreateEmployee(
                "EMP005",
                new DateOnly(2025, 6, 1),
                EmployeeStatus.Inactive,
                new DateOnly(2026, 4, 15));

            dbContext.Employees.AddRange(
                emp001, emp002, emp003, emp004, emp005);

            dbContext.EmploymentPeriods.AddRange(
                new EmploymentPeriod(
                    Guid.NewGuid(),
                    emp001.Id,
                    emp001.HireDate),
                new EmploymentPeriod(
                    Guid.NewGuid(),
                    emp002.Id,
                    emp002.HireDate),
                new EmploymentPeriod(
                    Guid.NewGuid(),
                    emp003.Id,
                    emp003.HireDate,
                    emp003.TerminationDate),
                new EmploymentPeriod(
                    Guid.NewGuid(),
                    emp004.Id,
                    emp004.HireDate),
                new EmploymentPeriod(
                    Guid.NewGuid(),
                    emp005.Id,
                    emp005.HireDate,
                    emp005.TerminationDate));

            await dbContext.SaveChangesAsync();
        }

        var service =
            new EfWorkforceAnalyticsService(
                new TestDbContextFactory(options));
        WorkforceMovementSummary summary =
            await service.GetWorkforceMovementAsync(
                2026,
                WorkforceAnalyticsGrouping.Quarterly);

        Assert.Equal(4, summary.Periods.Count);

        WorkforceMovementPeriod q1 =
            summary.Periods[0];
        Assert.Equal(2, q1.NewHires);
        Assert.Equal(1, q1.Separations);
        Assert.Equal(2, q1.BeginningHeadcount);
        Assert.Equal(3, q1.EndingHeadcount);
        Assert.Equal(2.5m, q1.AverageHeadcount);
        Assert.Equal(40m, q1.TurnoverRate);
        Assert.Equal(1, q1.NetChange);

        WorkforceMovementPeriod q2 =
            summary.Periods[1];
        Assert.Equal(1, q2.NewHires);
        Assert.Equal(1, q2.Separations);
        Assert.Equal(3, q2.BeginningHeadcount);
        Assert.Equal(3, q2.EndingHeadcount);
        Assert.Equal(3m, q2.AverageHeadcount);
        Assert.Equal(33.33m, q2.TurnoverRate);
        Assert.Equal(0, q2.NetChange);
    }

    [Fact]
    public async Task GetWorkforceMovementAsync_WhenDatabaseIsEmpty_ReturnsZeroPeriodsForWholeYear()
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
            await dbContext.Database
                .EnsureCreatedAsync();
        }

        var service =
            new EfWorkforceAnalyticsService(
                new TestDbContextFactory(options));
        WorkforceMovementSummary summary =
            await service.GetWorkforceMovementAsync(
                2026);

        Assert.Equal(12, summary.Periods.Count);
        Assert.Equal(0, summary.BeginningHeadcount);
        Assert.Equal(0, summary.EndingHeadcount);
        Assert.Equal(0, summary.TotalNewHires);
        Assert.Equal(0, summary.TotalSeparations);
        Assert.Equal(0, summary.NetChange);
        Assert.Equal(0m, summary.AverageHeadcount);
        Assert.Equal(0m, summary.TurnoverRate);
        Assert.All(
            summary.Periods,
            period =>
            {
                Assert.Equal(0, period.NewHires);
                Assert.Equal(0, period.Separations);
                Assert.Equal(0, period.BeginningHeadcount);
                Assert.Equal(0, period.EndingHeadcount);
                Assert.Equal(0m, period.TurnoverRate);
            });
    }

    [Fact]
    public async Task GetWorkforceMovementAsync_WhenEventsOccurOnPeriodBoundaries_KeepsHeadcountBalanced()
    {
        await using var connection =
            new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        DbContextOptions<HrManagementDbContext> options =
            new DbContextOptionsBuilder<HrManagementDbContext>()
                .UseSqlite(connection)
                .Options;

        await using (var dbContext =
                     new HrManagementDbContext(options))
        {
            await dbContext.Database.EnsureCreatedAsync();

            Employee emp001 = CreateEmployee(
                "EMP001",
                new DateOnly(2025, 12, 1),
                EmployeeStatus.Active);
            Employee emp002 = CreateEmployee(
                "EMP002",
                new DateOnly(2026, 1, 1),
                EmployeeStatus.Active);
            Employee emp003 = CreateEmployee(
                "EMP003",
                new DateOnly(2025, 1, 1),
                EmployeeStatus.Inactive,
                new DateOnly(2026, 1, 1));
            Employee emp004 = CreateEmployee(
                "EMP004",
                new DateOnly(2026, 1, 31),
                EmployeeStatus.Inactive,
                new DateOnly(2026, 1, 31));

            dbContext.Employees.AddRange(
                emp001, emp002, emp003, emp004);

            dbContext.EmploymentPeriods.AddRange(
                new EmploymentPeriod(
                    Guid.NewGuid(),
                    emp001.Id,
                    emp001.HireDate),
                new EmploymentPeriod(
                    Guid.NewGuid(),
                    emp002.Id,
                    emp002.HireDate),
                new EmploymentPeriod(
                    Guid.NewGuid(),
                    emp003.Id,
                    emp003.HireDate,
                    emp003.TerminationDate),
                new EmploymentPeriod(
                    Guid.NewGuid(),
                    emp004.Id,
                    emp004.HireDate,
                    emp004.TerminationDate));

            await dbContext.SaveChangesAsync();
        }

        var service =
            new EfWorkforceAnalyticsService(
                new TestDbContextFactory(options));
        WorkforceMovementSummary summary =
            await service.GetWorkforceMovementAsync(
                2026,
                WorkforceAnalyticsGrouping.Monthly);

        WorkforceMovementPeriod january =
            summary.Periods[0];
        Assert.Equal(2, january.BeginningHeadcount);
        Assert.Equal(2, january.NewHires);
        Assert.Equal(2, january.Separations);
        Assert.Equal(2, january.EndingHeadcount);
        Assert.Equal(
            january.BeginningHeadcount
            + january.NewHires
            - january.Separations,
            january.EndingHeadcount);
        Assert.Equal(2m, january.AverageHeadcount);
        Assert.Equal(100m, january.TurnoverRate);
    }

    [Fact]
    public async Task GetWorkforceMovementAsync_WhenYearIsInvalid_Throws()
    {
        var options =
            new DbContextOptionsBuilder<HrManagementDbContext>()
                .UseSqlite("Data Source=:memory:")
                .Options;
        var service =
            new EfWorkforceAnalyticsService(
                new TestDbContextFactory(options));
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => service.GetWorkforceMovementAsync(0));
    }

    private static Employee CreateEmployee(
        string employeeCode,
        DateOnly hireDate,
        EmployeeStatus status,
        DateOnly? terminationDate = null)
    {
        return new Employee(
            Guid.NewGuid(),
            employeeCode,
            $"Nhân viên {employeeCode}",
            "employee@example.com",
            "0901000000",
            new DateOnly(1995, 1, 1),
            hireDate,
            "Phòng ban kiểm thử",
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
    public async Task GetWorkforceMovementAsync_WhenEmployeeHasBeenRehired_CountsEachEmploymentPeriod()
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

        await using (var dbContext =
                     new HrManagementDbContext(options))
        {
            await dbContext.Database
                .EnsureCreatedAsync();

            var employee =
                new Employee(
                    employeeId,
                    "EMP-REHIRE-001",
                    "Nhân viên tái tuyển",
                    "employee@example.com",
                    "0901000000",
                    new DateOnly(1995, 1, 1),
                    new DateOnly(2022, 1, 1),
                    "Nhân sự",
                    "Chuyên viên",
                    EmployeeStatus.Active);

            dbContext.Employees.Add(employee);

            dbContext.EmploymentPeriods.AddRange(
                new EmploymentPeriod(
                    Guid.NewGuid(),
                    employeeId,
                    new DateOnly(2022, 1, 1),
                    new DateOnly(2026, 3, 15)),

                new EmploymentPeriod(
                    Guid.NewGuid(),
                    employeeId,
                    new DateOnly(2026, 8, 1)));

            await dbContext.SaveChangesAsync();
        }

        var service =
            new EfWorkforceAnalyticsService(
                new TestDbContextFactory(options));

        WorkforceMovementSummary summary =
            await service.GetWorkforceMovementAsync(
                2026,
                WorkforceAnalyticsGrouping.Monthly);

        Assert.Equal(
            1,
            summary.TotalNewHires);

        Assert.Equal(
            1,
            summary.TotalSeparations);

        WorkforceMovementPeriod march =
            summary.Periods[2];

        Assert.Equal(
            1,
            march.Separations);

        WorkforceMovementPeriod august =
            summary.Periods[7];

        Assert.Equal(
            1,
            august.NewHires);

        Assert.Equal(
            1,
            august.EndingHeadcount);
    }

    [Fact]
    public async Task GetWorkforceMovementAsync_AfterTerminationIsCancelled_DoesNotCountSeparation()
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

        await using (var dbContext =
                     new HrManagementDbContext(options))
        {
            await dbContext.Database
                .EnsureCreatedAsync();

            dbContext.Employees.Add(
                new Employee(
                    employeeId,
                    "EMP-CANCEL-ANALYTICS",
                    "Nhân viên kiểm thử",
                    null,
                    null,
                    null,
                    new DateOnly(2024, 1, 1),
                    "Nhân sự",
                    "Chuyên viên",
                    EmployeeStatus.Active));

            // Period đã reopen sau Cancel Deactivation.
            dbContext.EmploymentPeriods.Add(
                new EmploymentPeriod(
                    Guid.NewGuid(),
                    employeeId,
                    new DateOnly(2024, 1, 1)));

            await dbContext.SaveChangesAsync();
        }

        var service =
            new EfWorkforceAnalyticsService(
                new TestDbContextFactory(options));

        WorkforceMovementSummary summary =
            await service.GetWorkforceMovementAsync(
                2026);

        Assert.Equal(
            0,
            summary.TotalSeparations);

        Assert.Equal(
            1,
            summary.BeginningHeadcount);

        Assert.Equal(
            1,
            summary.EndingHeadcount);
    }
}
