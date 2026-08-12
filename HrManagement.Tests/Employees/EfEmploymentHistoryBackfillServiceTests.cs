using HrManagement.Application.Employees.EmploymentHistories;
using HrManagement.Domain.Employees;
using HrManagement.Infrastructure.Employees;
using HrManagement.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using static HrManagement.Tests.Employees.EfEmploymentHistoryRepositoryTests;

namespace HrManagement.Tests.Employees;
public sealed class EfEmploymentHistoryBackfillServiceTests
{
    [Fact]
    public async Task BackfillAsync_ForActiveAndOnLeaveEmployees_CreatesOpenPeriods()
    {
        await using var connection =
            new SqliteConnection(
                "Data Source=:memory:");

        await connection.OpenAsync();

        DbContextOptions<HrManagementDbContext> options =
            new DbContextOptionsBuilder<HrManagementDbContext>()
                .UseSqlite(connection)
                .Options;

        Guid activeEmployeeId =
            Guid.NewGuid();

        Guid onLeaveEmployeeId =
            Guid.NewGuid();

        await using (var dbContext =
                     new HrManagementDbContext(options))
        {
            await dbContext.Database.EnsureCreatedAsync();

            dbContext.Employees.AddRange(
                CreateEmployee(
                    activeEmployeeId,
                    "EMP-BF-001",
                    new DateOnly(2024, 1, 10),
                    EmployeeStatus.Active),

                CreateEmployee(
                    onLeaveEmployeeId,
                    "EMP-BF-002",
                    new DateOnly(2025, 3, 15),
                    EmployeeStatus.OnLeave));

            await dbContext.SaveChangesAsync();
        }

        var service =
            new EfEmploymentHistoryBackfillService(
                new TestDbContextFactory(options));

        EmploymentHistoryBackfillResult result =
            await service.BackfillAsync();

        Assert.Equal(2, result.ScannedEmployees);
        Assert.Equal(2, result.CreatedPeriods);
        Assert.Equal(0, result.SkippedExistingHistory);
        Assert.Equal(
            0,
            result.SkippedIncompleteLegacyRecords);

        await using var verificationContext =
            new HrManagementDbContext(options);

        List<EmploymentPeriod> periods =
            await verificationContext
                .EmploymentPeriods
                .OrderBy(period =>
                    period.StartDate)
                .ToListAsync();

        Assert.Equal(2, periods.Count);

        Assert.All(
            periods,
            period =>
                Assert.Null(period.EndDate));
    }

    [Fact]
    public async Task BackfillAsync_ForInactiveEmployeeWithTerminationDate_CreatesClosedPeriod()
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

        DateOnly hireDate =
            new(2022, 2, 1);

        DateOnly terminationDate =
            new(2026, 5, 20);

        await using (var dbContext =
                     new HrManagementDbContext(options))
        {
            await dbContext.Database.EnsureCreatedAsync();

            dbContext.Employees.Add(
                CreateEmployee(
                    employeeId,
                    "EMP-BF-003",
                    hireDate,
                    EmployeeStatus.Inactive,
                    terminationDate));

            await dbContext.SaveChangesAsync();
        }

        var service =
            new EfEmploymentHistoryBackfillService(
                new TestDbContextFactory(options));

        EmploymentHistoryBackfillResult result =
            await service.BackfillAsync();

        Assert.Equal(1, result.CreatedPeriods);

        await using var verificationContext =
            new HrManagementDbContext(options);

        EmploymentPeriod period =
            await verificationContext
                .EmploymentPeriods
                .SingleAsync();

        Assert.Equal(hireDate, period.StartDate);
        Assert.Equal(
            terminationDate,
            period.EndDate);

        Assert.False(period.IsOpen);
    }

    [Fact]
    public async Task BackfillAsync_ForLegacyInactiveWithoutTerminationDate_SkipsRecord()
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
                    "EMP-BF-004",
                    new DateOnly(2020, 1, 1),
                    EmployeeStatus.Inactive));

            await dbContext.SaveChangesAsync();
        }

        var service =
            new EfEmploymentHistoryBackfillService(
                new TestDbContextFactory(options));

        EmploymentHistoryBackfillResult result =
            await service.BackfillAsync();

        Assert.Equal(1, result.ScannedEmployees);
        Assert.Equal(0, result.CreatedPeriods);

        Assert.Equal(
            1,
            result.SkippedIncompleteLegacyRecords);

        await using var verificationContext =
            new HrManagementDbContext(options);

        Assert.Empty(
            await verificationContext
                .EmploymentPeriods
                .ToListAsync());
    }

    [Fact]
    public async Task BackfillAsync_WhenRunTwice_IsIdempotent()
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
                    "EMP-BF-005",
                    new DateOnly(2024, 6, 1),
                    EmployeeStatus.Active));

            await dbContext.SaveChangesAsync();
        }

        var service =
            new EfEmploymentHistoryBackfillService(
                new TestDbContextFactory(options));

        EmploymentHistoryBackfillResult first =
            await service.BackfillAsync();

        EmploymentHistoryBackfillResult second =
            await service.BackfillAsync();

        Assert.Equal(1, first.CreatedPeriods);
        Assert.Equal(0, second.CreatedPeriods);
        Assert.Equal(
            1,
            second.SkippedExistingHistory);

        await using var verificationContext =
            new HrManagementDbContext(options);

        Assert.Equal(
            1,
            await verificationContext
                .EmploymentPeriods
                .CountAsync());
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
}
