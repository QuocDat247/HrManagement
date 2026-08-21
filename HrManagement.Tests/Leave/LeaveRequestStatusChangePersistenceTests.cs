using HrManagement.Domain.Employees;
using HrManagement.Domain.Leave.Requests;
using HrManagement.Domain.Leave.Types;
using HrManagement.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace HrManagement.Tests.Leave;

public sealed class LeaveRequestStatusChangePersistenceTests
{
    [Fact]
    public async Task StatusChange_RoundTrip_PreservesValuesAndUtc()
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

        LeaveRequest request =
            CreateRequest(
                seed);

        DateTime changedAtUtc =
            Utc(
                2026,
                8,
                21,
                5,
                30);

        LeaveRequestStatusChange change =
            request.TransitionTo(
                Guid.NewGuid(),
                LeaveRequestStatus.Approved,
                changedAtUtc,
                "user-001",
                "admin",
                "  Đã kiểm tra hồ sơ  ");

        await using (
            var dbContext =
                new HrManagementDbContext(
                    options))
        {
            await dbContext.LeaveRequests.AddAsync(
                request);

            await dbContext
                .LeaveRequestStatusChanges
                .AddAsync(
                    change);

            await dbContext.SaveChangesAsync();
        }

        await using var verification =
            new HrManagementDbContext(
                options);

        LeaveRequestStatusChange saved =
            await verification
                .LeaveRequestStatusChanges
                .AsNoTracking()
                .SingleAsync();

        Assert.Equal(
            change.Id,
            saved.Id);

        Assert.Equal(
            request.Id,
            saved.LeaveRequestId);

        Assert.Equal(
            LeaveRequestStatus.Pending,
            saved.FromStatus);

        Assert.Equal(
            LeaveRequestStatus.Approved,
            saved.ToStatus);

        Assert.Equal(
            changedAtUtc,
            saved.ChangedAtUtc);

        Assert.Equal(
            DateTimeKind.Utc,
            saved.ChangedAtUtc.Kind);

        Assert.Equal(
            "user-001",
            saved.ChangedByUserId);

        Assert.Equal(
            "admin",
            saved.ChangedByUsername);

        Assert.Equal(
            "Đã kiểm tra hồ sơ",
            saved.Note);
    }

    [Fact]
    public async Task MultipleStatusChanges_CanBeStoredForSameRequest()
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

        LeaveRequest request =
            CreateRequest(
                seed);

        LeaveRequestStatusChange approval =
            request.TransitionTo(
                Guid.NewGuid(),
                LeaveRequestStatus.Approved,
                Utc(
                    2026,
                    8,
                    21,
                    5,
                    0),
                "user-001",
                "admin");

        LeaveRequestStatusChange cancellation =
            request.TransitionTo(
                Guid.NewGuid(),
                LeaveRequestStatus.Cancelled,
                Utc(
                    2026,
                    8,
                    21,
                    6,
                    0),
                "user-001",
                "admin",
                "Hủy theo yêu cầu");

        await using (
            var dbContext =
                new HrManagementDbContext(
                    options))
        {
            await dbContext.LeaveRequests.AddAsync(
                request);

            await dbContext
                .LeaveRequestStatusChanges
                .AddRangeAsync(
                    approval,
                    cancellation);

            await dbContext.SaveChangesAsync();
        }

        await using var verification =
            new HrManagementDbContext(
                options);

        List<LeaveRequestStatusChange> history =
            await verification
                .LeaveRequestStatusChanges
                .AsNoTracking()
                .Where(
                    change =>
                        change.LeaveRequestId ==
                        request.Id)
                .OrderBy(
                    change =>
                        change.ChangedAtUtc)
                .ThenBy(
                    change =>
                        change.Id)
                .ToListAsync();

        Assert.Equal(
            2,
            history.Count);

        Assert.Equal(
            LeaveRequestStatus.Approved,
            history[0].ToStatus);

        Assert.Equal(
            LeaveRequestStatus.Cancelled,
            history[1].ToStatus);
    }

    [Fact]
    public async Task StatusChangeWithoutLeaveRequest_IsRejectedByForeignKey()
    {
        await using SqliteConnection connection =
            await CreateOpenConnectionAsync();

        DbContextOptions<HrManagementDbContext> options =
            CreateOptions(
                connection);

        await EnsureCreatedAsync(
            options);

        var orphan =
            new LeaveRequestStatusChange(
                Guid.NewGuid(),
                Guid.NewGuid(),
                LeaveRequestStatus.Pending,
                LeaveRequestStatus.Approved,
                Utc(
                    2026,
                    8,
                    21,
                    5,
                    0),
                "user-001",
                "admin");

        await using var dbContext =
            new HrManagementDbContext(
                options);

        await dbContext
            .LeaveRequestStatusChanges
            .AddAsync(
                orphan);

        await Assert.ThrowsAsync<DbUpdateException>(
            () =>
                dbContext.SaveChangesAsync());
    }

    [Fact]
    public async Task LeaveRequestWithHistory_CannotBeDeleted()
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

        LeaveRequest request =
            CreateRequest(
                seed);

        LeaveRequestStatusChange change =
            request.TransitionTo(
                Guid.NewGuid(),
                LeaveRequestStatus.Approved,
                Utc(
                    2026,
                    8,
                    21,
                    5,
                    0),
                "user-001",
                "admin");

        await using (
            var dbContext =
                new HrManagementDbContext(
                    options))
        {
            await dbContext.LeaveRequests.AddAsync(
                request);

            await dbContext
                .LeaveRequestStatusChanges
                .AddAsync(
                    change);

            await dbContext.SaveChangesAsync();
        }

        await using var deletion =
            new HrManagementDbContext(
                options);

        LeaveRequest persistedRequest =
            await deletion
                .LeaveRequests
                .SingleAsync(
                    item =>
                        item.Id ==
                        request.Id);

        deletion.LeaveRequests.Remove(
            persistedRequest);

        await Assert.ThrowsAsync<DbUpdateException>(
            () =>
                deletion.SaveChangesAsync());
    }

    private static LeaveRequest CreateRequest(
        SeedContext seed)
    {
        return new LeaveRequest(
            Guid.NewGuid(),
            seed.EmployeeId,
            seed.EmploymentPeriodId,
            seed.LeaveTypeId,
            new DateOnly(
                2026,
                8,
                25),
            new DateOnly(
                2026,
                8,
                26),
            null,
            Utc(
                2026,
                8,
                20,
                4,
                0));
    }

    private static async Task<SeedContext>
        SeedContextAsync(
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
