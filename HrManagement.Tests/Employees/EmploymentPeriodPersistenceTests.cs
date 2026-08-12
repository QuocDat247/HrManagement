using HrManagement.Domain.Employees;
using HrManagement.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace HrManagement.Tests.Employees;
public sealed class EmploymentPeriodPersistenceTests
{
    [Fact]
    public async Task EmploymentPeriod_CanBePersistedAndReloaded()
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

        await using (var dbContext =
                     new HrManagementDbContext(options))
        {
            await dbContext.Database.EnsureCreatedAsync();

            dbContext.Employees.Add(
                CreateEmployee(
                    employeeId,
                    "EMP-PERIOD-001"));

            dbContext.EmploymentPeriods.Add(
                new EmploymentPeriod(
                    periodId,
                    employeeId,
                    new DateOnly(2026, 1, 10)));

            await dbContext.SaveChangesAsync();
        }

        await using (var dbContext =
                     new HrManagementDbContext(options))
        {
            EmploymentPeriod period =
                await dbContext.EmploymentPeriods
                    .SingleAsync();

            Assert.Equal(periodId, period.Id);
            Assert.Equal(employeeId, period.EmployeeId);
            Assert.Equal(
                new DateOnly(2026, 1, 10),
                period.StartDate);

            Assert.Null(period.EndDate);
            Assert.True(period.IsOpen);
        }
    }

    [Fact]
    public async Task Database_WhenEmployeeHasTwoOpenPeriods_RejectsSecondPeriod()
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

        await using var dbContext =
            new HrManagementDbContext(options);

        await dbContext.Database.EnsureCreatedAsync();

        dbContext.Employees.Add(
            CreateEmployee(
                employeeId,
                "EMP-PERIOD-002"));

        dbContext.EmploymentPeriods.Add(
            new EmploymentPeriod(
                Guid.NewGuid(),
                employeeId,
                new DateOnly(2025, 1, 1)));

        await dbContext.SaveChangesAsync();

        dbContext.EmploymentPeriods.Add(
            new EmploymentPeriod(
                Guid.NewGuid(),
                employeeId,
                new DateOnly(2026, 1, 1)));

        await Assert.ThrowsAsync<DbUpdateException>(
            () => dbContext.SaveChangesAsync());
    }

    [Fact]
    public async Task Database_AllowsClosedHistoryAndOneOpenPeriod()
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

        await using var dbContext =
            new HrManagementDbContext(options);

        await dbContext.Database.EnsureCreatedAsync();

        dbContext.Employees.Add(
            CreateEmployee(
                employeeId,
                "EMP-PERIOD-003"));

        dbContext.EmploymentPeriods.AddRange(
            new EmploymentPeriod(
                Guid.NewGuid(),
                employeeId,
                new DateOnly(2022, 1, 1),
                new DateOnly(2023, 12, 31)),

            new EmploymentPeriod(
                Guid.NewGuid(),
                employeeId,
                new DateOnly(2024, 2, 1),
                new DateOnly(2025, 6, 15)),

            new EmploymentPeriod(
                Guid.NewGuid(),
                employeeId,
                new DateOnly(2026, 1, 10)));

        await dbContext.SaveChangesAsync();

        int count =
            await dbContext.EmploymentPeriods.CountAsync();

        Assert.Equal(3, count);
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
}
