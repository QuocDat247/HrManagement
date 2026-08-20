using HrManagement.Domain.Employees;
using HrManagement.Domain.Leave.Requests;
using HrManagement.Domain.Leave.Types;
using HrManagement.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace HrManagement.Tests.Leave;

public sealed class LeavePersistenceTests
{
    [Fact]
    public async Task LeaveType_RoundTrip_PreservesValues()
    {
        await using SqliteConnection connection =
            await CreateOpenConnectionAsync();

        DbContextOptions<HrManagementDbContext> options =
            CreateOptions(
                connection);

        await EnsureCreatedAsync(
            options);

        var leaveType =
            new LeaveType(
                Guid.NewGuid(),
                " annual ",
                " Nghỉ phép năm ",
                isPaid: true);

        await using (
            var dbContext =
                new HrManagementDbContext(
                    options))
        {
            await dbContext.LeaveTypes.AddAsync(
                leaveType);

            await dbContext.SaveChangesAsync();
        }

        await using var verification =
            new HrManagementDbContext(
                options);

        LeaveType saved =
            await verification
                .LeaveTypes
                .AsNoTracking()
                .SingleAsync();

        Assert.Equal(
            leaveType.Id,
            saved.Id);

        Assert.Equal(
            "ANNUAL",
            saved.Code);

        Assert.Equal(
            "Nghỉ phép năm",
            saved.Name);

        Assert.True(
            saved.IsPaid);

        Assert.True(
            saved.IsActive);
    }

    [Fact]
    public async Task LeaveRequest_RoundTrip_PreservesPendingAndUtc()
    {
        await using SqliteConnection connection =
            await CreateOpenConnectionAsync();

        DbContextOptions<HrManagementDbContext> options =
            CreateOptions(
                connection);

        await EnsureCreatedAsync(
            options);

        SeedContext seed =
            await SeedContextAsync(
                options);

        DateTime submittedAtUtc =
            Utc(
                2026,
                8,
                20,
                3,
                15);

        var request =
            new LeaveRequest(
                Guid.NewGuid(),
                seed.EmployeeId,
                seed.EmploymentPeriodId,
                seed.LeaveTypeId,
                new DateOnly(
                    2026,
                    8,
                    24),
                new DateOnly(
                    2026,
                    8,
                    26),
                "  Việc gia đình  ",
                submittedAtUtc);

        await using (
            var dbContext =
                new HrManagementDbContext(
                    options))
        {
            await dbContext.LeaveRequests.AddAsync(
                request);

            await dbContext.SaveChangesAsync();
        }

        await using var verification =
            new HrManagementDbContext(
                options);

        LeaveRequest saved =
            await verification
                .LeaveRequests
                .AsNoTracking()
                .SingleAsync();

        Assert.Equal(
            request.Id,
            saved.Id);

        Assert.Equal(
            seed.EmployeeId,
            saved.EmployeeId);

        Assert.Equal(
            seed.EmploymentPeriodId,
            saved.EmploymentPeriodId);

        Assert.Equal(
            seed.LeaveTypeId,
            saved.LeaveTypeId);

        Assert.Equal(
            new DateOnly(
                2026,
                8,
                24),
            saved.StartDate);

        Assert.Equal(
            new DateOnly(
                2026,
                8,
                26),
            saved.EndDate);

        Assert.Equal(
            "Việc gia đình",
            saved.Reason);

        Assert.Equal(
            submittedAtUtc,
            saved.SubmittedAtUtc);

        Assert.Equal(
            DateTimeKind.Utc,
            saved.SubmittedAtUtc.Kind);

        Assert.Equal(
            LeaveRequestStatus.Pending,
            saved.Status);
    }

    [Fact]
    public async Task DuplicateLeaveTypeCode_IsRejected()
    {
        await using SqliteConnection connection =
            await CreateOpenConnectionAsync();

        DbContextOptions<HrManagementDbContext> options =
            CreateOptions(
                connection);

        await EnsureCreatedAsync(
            options);

        await using var dbContext =
            new HrManagementDbContext(
                options);

        await dbContext.LeaveTypes.AddAsync(
            new LeaveType(
                Guid.NewGuid(),
                "annual",
                "Nghỉ phép năm",
                isPaid: true));

        await dbContext.SaveChangesAsync();

        await dbContext.LeaveTypes.AddAsync(
            new LeaveType(
                Guid.NewGuid(),
                "ANNUAL",
                "Annual Leave",
                isPaid: true));

        await Assert.ThrowsAsync<DbUpdateException>(
            () =>
                dbContext.SaveChangesAsync());
    }

    [Fact]
    public async Task ReferencedLeaveType_CannotBeDeleted()
    {
        await using SqliteConnection connection =
            await CreateOpenConnectionAsync();

        DbContextOptions<HrManagementDbContext> options =
            CreateOptions(
                connection);

        await EnsureCreatedAsync(
            options);

        SeedContext seed =
            await SeedContextAsync(
                options);

        await SeedLeaveRequestAsync(
            options,
            seed);

        await using var dbContext =
            new HrManagementDbContext(
                options);

        LeaveType leaveType =
            await dbContext
                .LeaveTypes
                .SingleAsync(
                    item =>
                        item.Id ==
                        seed.LeaveTypeId);

        dbContext.LeaveTypes.Remove(
            leaveType);

        await Assert.ThrowsAsync<DbUpdateException>(
            () =>
                dbContext.SaveChangesAsync());
    }

    [Fact]
    public async Task ReferencedEmploymentPeriod_CannotBeDeleted()
    {
        await using SqliteConnection connection =
            await CreateOpenConnectionAsync();

        DbContextOptions<HrManagementDbContext> options =
            CreateOptions(
                connection);

        await EnsureCreatedAsync(
            options);

        SeedContext seed =
            await SeedContextAsync(
                options);

        await SeedLeaveRequestAsync(
            options,
            seed);

        await using var dbContext =
            new HrManagementDbContext(
                options);

        EmploymentPeriod period =
            await dbContext
                .EmploymentPeriods
                .SingleAsync(
                    item =>
                        item.Id ==
                        seed.EmploymentPeriodId);

        dbContext.EmploymentPeriods.Remove(
            period);

        await Assert.ThrowsAsync<DbUpdateException>(
            () =>
                dbContext.SaveChangesAsync());
    }

    private static async Task SeedLeaveRequestAsync(
        DbContextOptions<HrManagementDbContext> options,
        SeedContext seed)
    {
        await using var dbContext =
            new HrManagementDbContext(
                options);

        await dbContext.LeaveRequests.AddAsync(
            new LeaveRequest(
                Guid.NewGuid(),
                seed.EmployeeId,
                seed.EmploymentPeriodId,
                seed.LeaveTypeId,
                new DateOnly(
                    2026,
                    8,
                    24),
                new DateOnly(
                    2026,
                    8,
                    25),
                null,
                Utc(
                    2026,
                    8,
                    20,
                    3,
                    0)));

        await dbContext.SaveChangesAsync();
    }

    private static async Task<SeedContext> SeedContextAsync(
        DbContextOptions<HrManagementDbContext> options)
    {
        Guid employeeId =
            Guid.NewGuid();

        Guid employmentPeriodId =
            Guid.NewGuid();

        Guid leaveTypeId =
            Guid.NewGuid();

        await using var dbContext =
            new HrManagementDbContext(
                options);

        await dbContext.Employees.AddAsync(
            new Employee(
                employeeId,
                $"EMP{employeeId:N}"[..20],
                "Nhân viên kiểm thử",
                null,
                null,
                null,
                new DateOnly(
                    2025,
                    1,
                    1),
                "Phòng kiểm thử",
                "Chuyên viên kiểm thử",
                EmployeeStatus.Active));

        await dbContext.EmploymentPeriods.AddAsync(
            new EmploymentPeriod(
                employmentPeriodId,
                employeeId,
                new DateOnly(
                    2026,
                    1,
                    1)));

        await dbContext.LeaveTypes.AddAsync(
            new LeaveType(
                leaveTypeId,
                "ANNUAL",
                "Nghỉ phép năm",
                isPaid: true));

        await dbContext.SaveChangesAsync();

        return new SeedContext(
            employeeId,
            employmentPeriodId,
            leaveTypeId);
    }

    private static DateTime Utc(
        int year,
        int month,
        int day,
        int hour,
        int minute)
    {
        return new DateTime(
            year,
            month,
            day,
            hour,
            minute,
            0,
            DateTimeKind.Utc);
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
        return new DbContextOptionsBuilder<
                HrManagementDbContext>()
            .UseSqlite(
                connection)
            .Options;
    }

    private static async Task EnsureCreatedAsync(
        DbContextOptions<HrManagementDbContext> options)
    {
        await using var dbContext =
            new HrManagementDbContext(
                options);

        await dbContext.Database
            .EnsureCreatedAsync();
    }

    private sealed record SeedContext(
        Guid EmployeeId,
        Guid EmploymentPeriodId,
        Guid LeaveTypeId);
}
