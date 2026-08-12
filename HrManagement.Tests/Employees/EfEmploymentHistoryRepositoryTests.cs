using HrManagement.Application.Employees.EmploymentHistories;
using HrManagement.Domain.Employees;
using HrManagement.Infrastructure.Employees;
using HrManagement.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace HrManagement.Tests.Employees;
public sealed class EfEmploymentHistoryRepositoryTests
{
    [Fact]
    public async Task GetByEmployeeIdAsync_WhenNoPeriodsExist_ReturnsEmptyHistory()
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
            await dbContext.Database.EnsureCreatedAsync();

            dbContext.Employees.Add(
                CreateEmployee(
                    employeeId,
                    "EMP-HISTORY-001"));

            await dbContext.SaveChangesAsync();
        }

        var repository =
            new EfEmploymentHistoryRepository(
                new TestDbContextFactory(options));

        EmploymentHistory history =
            await repository.GetByEmployeeIdAsync(
                employeeId);

        Assert.Equal(
            employeeId,
            history.EmployeeId);

        Assert.Empty(
            history.Periods);

        Assert.Null(
            history.CurrentPeriod);
    }

    [Fact]
    public async Task AddPeriodAsync_ThenGetByEmployeeIdAsync_ReturnsPersistedHistory()
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
            await dbContext.Database.EnsureCreatedAsync();

            dbContext.Employees.Add(
                CreateEmployee(
                    employeeId,
                    "EMP-HISTORY-002"));

            await dbContext.SaveChangesAsync();
        }

        var repository =
            new EfEmploymentHistoryRepository(
                new TestDbContextFactory(options));

        var closedPeriod =
            new EmploymentPeriod(
                Guid.NewGuid(),
                employeeId,
                new DateOnly(2022, 1, 1),
                new DateOnly(2024, 12, 31));

        var openPeriod =
            new EmploymentPeriod(
                Guid.NewGuid(),
                employeeId,
                new DateOnly(2025, 2, 1));

        await repository.AddPeriodAsync(
            closedPeriod);

        await repository.AddPeriodAsync(
            openPeriod);

        EmploymentHistory history =
            await repository.GetByEmployeeIdAsync(
                employeeId);

        Assert.Equal(
            2,
            history.Periods.Count);

        Assert.Equal(
            closedPeriod.Id,
            history.Periods[0].Id);

        Assert.Equal(
            openPeriod.Id,
            history.Periods[1].Id);

        Assert.Equal(
            openPeriod.Id,
            history.CurrentPeriod?.Id);
    }

    [Fact]
    public async Task AddPeriodAsync_WhenSecondOpenPeriodIsAdded_Throws()
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
            await dbContext.Database.EnsureCreatedAsync();

            dbContext.Employees.Add(
                CreateEmployee(
                    employeeId,
                    "EMP-HISTORY-003"));

            await dbContext.SaveChangesAsync();
        }

        var repository =
            new EfEmploymentHistoryRepository(
                new TestDbContextFactory(options));

        await repository.AddPeriodAsync(
            new EmploymentPeriod(
                Guid.NewGuid(),
                employeeId,
                new DateOnly(2025, 1, 1)));

        await Assert.ThrowsAsync<DbUpdateException>(
            () => repository.AddPeriodAsync(
                new EmploymentPeriod(
                    Guid.NewGuid(),
                    employeeId,
                    new DateOnly(2026, 1, 1))));
    }

    private static Employee CreateEmployee(
    Guid employeeId,
    string employeeCode)
    {
        return new Employee(
            employeeId,
            employeeCode,
            $"Nhân viên {employeeCode}",
            "employee@example.com",
            "0901000000",
            new DateOnly(1995, 1, 1),
            new DateOnly(2022, 1, 1),
            "Kiểm thử",
            "Nhân viên",
            EmployeeStatus.Active);
    }

    public sealed class TestDbContextFactory : IDbContextFactory<HrManagementDbContext>
    {
    private readonly DbContextOptions<HrManagementDbContext> _options;

    public TestDbContextFactory(DbContextOptions<HrManagementDbContext> options)
    {
        _options = options;
    }

    public HrManagementDbContext CreateDbContext()
    {
        return new HrManagementDbContext(_options);
    }
    }
    [Fact]
    public async Task UpdatePeriodAsync_AfterClosingPeriod_PersistsEndDate()
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
            await dbContext.Database.EnsureCreatedAsync();

            dbContext.Employees.Add(
                CreateEmployee(
                    employeeId,
                    "EMP-HISTORY-004"));

            await dbContext.SaveChangesAsync();
        }

        var repository =
            new EfEmploymentHistoryRepository(
                new TestDbContextFactory(options));

        var period =
            new EmploymentPeriod(
                Guid.NewGuid(),
                employeeId,
                new DateOnly(2025, 1, 10));

        await repository.AddPeriodAsync(period);

        EmploymentHistory history =
            await repository.GetByEmployeeIdAsync(
                employeeId);

        DateOnly terminationDate =
            new(2026, 8, 12);

        EmploymentPeriod closedPeriod =
            history.CloseCurrentPeriod(
                terminationDate);

        await repository.UpdatePeriodAsync(
            closedPeriod);

        EmploymentHistory reloadedHistory =
            await repository.GetByEmployeeIdAsync(
                employeeId);

        EmploymentPeriod persistedPeriod =
            Assert.Single(
                reloadedHistory.Periods);

        Assert.Equal(
            terminationDate,
            persistedPeriod.EndDate);

        Assert.False(
            persistedPeriod.IsOpen);

        Assert.Null(
            reloadedHistory.CurrentPeriod);
    }
}
