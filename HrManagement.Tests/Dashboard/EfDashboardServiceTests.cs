using HrManagement.Domain.Employees;
using HrManagement.Infrastructure.Dashboard;
using HrManagement.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace HrManagement.Tests.Dashboard;

public sealed class EfDashboardServiceTests
{
    [Fact]
    public async Task GetSummaryAsync_ReturnsEmployeeCountsByStatus()
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

            dbContext.Employees.AddRange(
                CreateEmployee(
                    "EMP001",
                    EmployeeStatus.Active),
                CreateEmployee(
                    "EMP002",
                    EmployeeStatus.Active),
                CreateEmployee(
                    "EMP003",
                    EmployeeStatus.OnLeave),
                CreateEmployee(
                    "EMP004",
                    EmployeeStatus.Inactive));

            await dbContext.SaveChangesAsync();
        }

        var factory =
            new TestDbContextFactory(options);

        var service =
            new EfDashboardService(factory);

        var summary =
            await service.GetSummaryAsync();

        Assert.Equal(4, summary.TotalEmployees);
        Assert.Equal(2, summary.ActiveEmployees);
        Assert.Equal(1, summary.EmployeesOnLeave);
        Assert.Equal(1, summary.InactiveEmployees);
    }

    [Fact]
    public async Task GetSummaryAsync_WhenDatabaseIsEmpty_ReturnsZeros()
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
        }

        var factory =
            new TestDbContextFactory(options);

        var service =
            new EfDashboardService(factory);

        var summary =
            await service.GetSummaryAsync();

        Assert.Equal(0, summary.TotalEmployees);
        Assert.Equal(0, summary.ActiveEmployees);
        Assert.Equal(0, summary.EmployeesOnLeave);
        Assert.Equal(0, summary.InactiveEmployees);
        Assert.Empty(summary.RecentEmployees);
    }

    private static Employee CreateEmployee(
        string employeeCode,
        EmployeeStatus status,
        DateOnly? hireDate = null)
    {
        return new Employee(
            Guid.NewGuid(),
            employeeCode,
            $"Nhân viên {employeeCode}",
            null,
            null,
            null,
            hireDate ?? new DateOnly(2024, 1, 1),
            "Phòng ban kiểm thử",
            "Nhân viên",
            status);
    }

    private sealed class TestDbContextFactory
        : IDbContextFactory<HrManagementDbContext>
    {
        private readonly DbContextOptions<HrManagementDbContext> _options;

        public TestDbContextFactory(
            DbContextOptions<HrManagementDbContext> options)
        {
            _options = options;
        }

        public HrManagementDbContext CreateDbContext()
        {
            return new HrManagementDbContext(_options);
        }
    }

    [Fact]
    public async Task GetSummaryAsync_ReturnsFiveMostRecentlyHiredEmployeesInOrder()
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

            dbContext.Employees.AddRange(
                CreateEmployee(
                    "EMP006",
                    EmployeeStatus.Active,
                    new DateOnly(2025, 7, 1)),

                CreateEmployee(
                    "EMP002",
                    EmployeeStatus.OnLeave,
                    new DateOnly(2025, 8, 1)),

                CreateEmployee(
                    "EMP001",
                    EmployeeStatus.Inactive,
                    new DateOnly(2025, 8, 1)),

                CreateEmployee(
                    "EMP005",
                    EmployeeStatus.Active,
                    new DateOnly(2024, 12, 1)),

                CreateEmployee(
                    "EMP003",
                    EmployeeStatus.Active,
                    new DateOnly(2025, 6, 15)),

                CreateEmployee(
                    "EMP004",
                    EmployeeStatus.Active,
                    new DateOnly(2025, 5, 1)));

            await dbContext.SaveChangesAsync();
        }

        var factory =
            new TestDbContextFactory(options);

        var service =
            new EfDashboardService(factory);

        var summary =
            await service.GetSummaryAsync();

        Assert.Equal(5, summary.RecentEmployees.Count);

        Assert.Collection(
            summary.RecentEmployees,

            employee =>
                Assert.Equal(
                    "EMP001",
                    employee.EmployeeCode),

            employee =>
                Assert.Equal(
                    "EMP002",
                    employee.EmployeeCode),

            employee =>
                Assert.Equal(
                    "EMP006",
                    employee.EmployeeCode),

            employee =>
                Assert.Equal(
                    "EMP003",
                    employee.EmployeeCode),

            employee =>
                Assert.Equal(
                    "EMP004",
                    employee.EmployeeCode));

        Assert.DoesNotContain(
            summary.RecentEmployees,
            employee =>
                employee.EmployeeCode == "EMP005");

        Assert.Equal(
            EmployeeStatus.Inactive,
            summary.RecentEmployees[0].Status);
    }
}
