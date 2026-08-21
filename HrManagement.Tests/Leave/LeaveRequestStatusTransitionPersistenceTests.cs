using HrManagement.Application.Leave.Requests;
using HrManagement.Domain.Employees;
using HrManagement.Domain.Leave.Requests;
using HrManagement.Domain.Leave.Types;
using HrManagement.Infrastructure.Leave.Requests;
using HrManagement.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace HrManagement.Tests.Leave;

public sealed class LeaveRequestStatusTransitionPersistenceTests
{
    [Fact]
    public async Task PendingToApproved_UpdatesStatusAndAppendsHistory()
    {
        await using SqliteConnection connection =
            await CreateOpenConnectionAsync();

        DbContextOptions<HrManagementDbContext> options =
            CreateOptions(
                connection);

        await EnsureCreatedAsync(
            options);

        SeedContext seed =
            await SeedPendingRequestAsync(
                options);

        LeaveRequestStatusChange change =
            CreateChange(
                seed.LeaveRequestId,
                LeaveRequestStatus.Pending,
                LeaveRequestStatus.Approved,
                Utc(
                    2026,
                    8,
                    21,
                    6,
                    0));

        ILeaveRequestStatusTransitionPersistence persistence =
            CreatePersistence(
                options);

        await persistence.ApplyAsync(
            change);

        await using var verification =
            new HrManagementDbContext(
                options);

        LeaveRequest request =
            await verification
                .LeaveRequests
                .AsNoTracking()
                .SingleAsync(
                    item =>
                        item.Id ==
                        seed.LeaveRequestId);

        Assert.Equal(
            LeaveRequestStatus.Approved,
            request.Status);

        LeaveRequestStatusChange savedChange =
            await verification
                .LeaveRequestStatusChanges
                .AsNoTracking()
                .SingleAsync();

        Assert.Equal(
            change.Id,
            savedChange.Id);

        Assert.Equal(
            LeaveRequestStatus.Pending,
            savedChange.FromStatus);

        Assert.Equal(
            LeaveRequestStatus.Approved,
            savedChange.ToStatus);
    }

    [Fact]
    public async Task ApprovedToCancelled_AppendsSecondHistoryEntry()
    {
        await using SqliteConnection connection =
            await CreateOpenConnectionAsync();

        DbContextOptions<HrManagementDbContext> options =
            CreateOptions(
                connection);

        await EnsureCreatedAsync(
            options);

        SeedContext seed =
            await SeedPendingRequestAsync(
                options);

        ILeaveRequestStatusTransitionPersistence persistence =
            CreatePersistence(
                options);

        LeaveRequestStatusChange approval =
            CreateChange(
                seed.LeaveRequestId,
                LeaveRequestStatus.Pending,
                LeaveRequestStatus.Approved,
                Utc(
                    2026,
                    8,
                    21,
                    6,
                    0));

        await persistence.ApplyAsync(
            approval);

        LeaveRequestStatusChange cancellation =
            CreateChange(
                seed.LeaveRequestId,
                LeaveRequestStatus.Approved,
                LeaveRequestStatus.Cancelled,
                Utc(
                    2026,
                    8,
                    21,
                    7,
                    0));

        await persistence.ApplyAsync(
            cancellation);

        await using var verification =
            new HrManagementDbContext(
                options);

        LeaveRequest request =
            await verification
                .LeaveRequests
                .AsNoTracking()
                .SingleAsync(
                    item =>
                        item.Id ==
                        seed.LeaveRequestId);

        Assert.Equal(
            LeaveRequestStatus.Cancelled,
            request.Status);

        List<LeaveRequestStatusChange> history =
            await verification
                .LeaveRequestStatusChanges
                .AsNoTracking()
                .OrderBy(
                    item =>
                        item.ChangedAtUtc)
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
    public async Task StaleFromStatus_ThrowsAndDoesNotAppendHistory()
    {
        await using SqliteConnection connection =
            await CreateOpenConnectionAsync();

        DbContextOptions<HrManagementDbContext> options =
            CreateOptions(
                connection);

        await EnsureCreatedAsync(
            options);

        SeedContext seed =
            await SeedPendingRequestAsync(
                options);

        ILeaveRequestStatusTransitionPersistence persistence =
            CreatePersistence(
                options);

        LeaveRequestStatusChange approval =
            CreateChange(
                seed.LeaveRequestId,
                LeaveRequestStatus.Pending,
                LeaveRequestStatus.Approved,
                Utc(
                    2026,
                    8,
                    21,
                    6,
                    0));

        await persistence.ApplyAsync(
            approval);

        LeaveRequestStatusChange staleRejection =
            CreateChange(
                seed.LeaveRequestId,
                LeaveRequestStatus.Pending,
                LeaveRequestStatus.Rejected,
                Utc(
                    2026,
                    8,
                    21,
                    6,
                    30));

        await Assert.ThrowsAsync<
            DbUpdateConcurrencyException>(
                () =>
                    persistence.ApplyAsync(
                        staleRejection));

        await using var verification =
            new HrManagementDbContext(
                options);

        LeaveRequest request =
            await verification
                .LeaveRequests
                .AsNoTracking()
                .SingleAsync(
                    item =>
                        item.Id ==
                        seed.LeaveRequestId);

        Assert.Equal(
            LeaveRequestStatus.Approved,
            request.Status);

        List<LeaveRequestStatusChange> history =
            await verification
                .LeaveRequestStatusChanges
                .AsNoTracking()
                .ToListAsync();

        LeaveRequestStatusChange onlyChange =
            Assert.Single(
                history);

        Assert.Equal(
            approval.Id,
            onlyChange.Id);
    }

    [Fact]
    public async Task MissingRequest_ThrowsAndDoesNotAppendHistory()
    {
        await using SqliteConnection connection =
            await CreateOpenConnectionAsync();

        DbContextOptions<HrManagementDbContext> options =
            CreateOptions(
                connection);

        await EnsureCreatedAsync(
            options);

        ILeaveRequestStatusTransitionPersistence persistence =
            CreatePersistence(
                options);

        LeaveRequestStatusChange change =
            CreateChange(
                Guid.NewGuid(),
                LeaveRequestStatus.Pending,
                LeaveRequestStatus.Approved,
                Utc(
                    2026,
                    8,
                    21,
                    6,
                    0));

        await Assert.ThrowsAsync<
            DbUpdateConcurrencyException>(
                () =>
                    persistence.ApplyAsync(
                        change));

        await using var verification =
            new HrManagementDbContext(
                options);

        Assert.Empty(
            await verification
                .LeaveRequestStatusChanges
                .ToListAsync());
    }

    [Fact]
    public async Task HistoryConflict_RollsBackStatusUpdate()
    {
        await using SqliteConnection connection =
            await CreateOpenConnectionAsync();

        DbContextOptions<HrManagementDbContext> options =
            CreateOptions(
                connection);

        await EnsureCreatedAsync(
            options);

        SeedContext seed =
            await SeedPendingRequestAsync(
                options);

        Guid duplicateHistoryId =
            Guid.NewGuid();

        LeaveRequestStatusChange existingHistory =
            new(
                duplicateHistoryId,
                seed.LeaveRequestId,
                LeaveRequestStatus.Pending,
                LeaveRequestStatus.Rejected,
                Utc(
                    2026,
                    8,
                    21,
                    5,
                    0),
                "user-existing",
                "existing-user");

        await using (
            var dbContext =
                new HrManagementDbContext(
                    options))
        {
            await dbContext
                .LeaveRequestStatusChanges
                .AddAsync(
                    existingHistory);

            await dbContext.SaveChangesAsync();
        }

        LeaveRequestStatusChange candidate =
            new(
                duplicateHistoryId,
                seed.LeaveRequestId,
                LeaveRequestStatus.Pending,
                LeaveRequestStatus.Approved,
                Utc(
                    2026,
                    8,
                    21,
                    6,
                    0),
                "user-001",
                "admin");

        ILeaveRequestStatusTransitionPersistence persistence =
            CreatePersistence(
                options);

        await Assert.ThrowsAsync<
            DbUpdateConcurrencyException>(
                () =>
                    persistence.ApplyAsync(
                        candidate));

        await using var verification =
            new HrManagementDbContext(
                options);

        LeaveRequest request =
            await verification
                .LeaveRequests
                .AsNoTracking()
                .SingleAsync(
                    item =>
                        item.Id ==
                        seed.LeaveRequestId);

        Assert.Equal(
            LeaveRequestStatus.Pending,
            request.Status);

        LeaveRequestStatusChange onlyHistory =
            Assert.Single(
                await verification
                    .LeaveRequestStatusChanges
                    .AsNoTracking()
                    .ToListAsync());

        Assert.Equal(
            existingHistory.Id,
            onlyHistory.Id);
    }

    private static ILeaveRequestStatusTransitionPersistence
        CreatePersistence(
            DbContextOptions<HrManagementDbContext> options)
    {
        return new EfLeaveRequestStatusTransitionPersistence(
            new TestDbContextFactory(
                options));
    }

    private static LeaveRequestStatusChange CreateChange(
        Guid leaveRequestId,
        LeaveRequestStatus fromStatus,
        LeaveRequestStatus toStatus,
        DateTime changedAtUtc)
    {
        return new LeaveRequestStatusChange(
            Guid.NewGuid(),
            leaveRequestId,
            fromStatus,
            toStatus,
            changedAtUtc,
            "user-001",
            "admin",
            "Kiểm thử");
    }

    private static async Task<SeedContext>
        SeedPendingRequestAsync(
            DbContextOptions<HrManagementDbContext> options)
    {
        Guid employeeId =
            Guid.NewGuid();

        Guid employmentPeriodId =
            Guid.NewGuid();

        Guid leaveTypeId =
            Guid.NewGuid();

        Guid leaveRequestId =
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

        await dbContext.LeaveRequests.AddAsync(
            new LeaveRequest(
                leaveRequestId,
                employeeId,
                employmentPeriodId,
                leaveTypeId,
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
                    0)));

        await dbContext.SaveChangesAsync();

        return new SeedContext(
            employeeId,
            employmentPeriodId,
            leaveTypeId,
            leaveRequestId);
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
        Guid LeaveTypeId,
        Guid LeaveRequestId);

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
}
