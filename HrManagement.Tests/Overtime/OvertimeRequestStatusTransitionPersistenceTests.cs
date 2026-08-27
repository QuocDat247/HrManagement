using HrManagement.Application.Auditing;
using HrManagement.Application.Overtime.Requests;
using HrManagement.Domain.Auditing;
using HrManagement.Domain.Employees;
using HrManagement.Domain.Overtime.Requests;
using HrManagement.Infrastructure.Overtime.Requests;
using HrManagement.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace HrManagement.Tests.Overtime;

public sealed class OvertimeRequestStatusTransitionPersistenceTests
{
    [Fact]
    public async Task ContextSource_ReturnsPersistedRequest()
    {
        await using TestDatabase database =
            await TestDatabase.CreateAsync();

        SeedResult seed =
            await database.SeedPendingRequestAsync();

        var source =
            new EfOvertimeRequestStatusTransitionContextSource(
                database.Factory);

        OvertimeRequest? request =
            await source.GetByIdAsync(
                seed.Request.Id);

        Assert.NotNull(
            request);

        Assert.Equal(
            OvertimeRequestStatus.Pending,
            request.Status);

        Assert.Equal(
            seed.Request.Id,
            request.Id);
    }

    [Fact]
    public async Task ApplyAsync_Approve_PersistsStatusHistoryAndAudit()
    {
        await using TestDatabase database =
            await TestDatabase.CreateAsync();

        SeedResult seed =
            await database.SeedPendingRequestAsync();

        OvertimeRequestStatusChange statusChange =
            seed.Request.TransitionTo(
                Guid.NewGuid(),
                OvertimeRequestStatus.Approved,
                Utc(
                    14),
                "user-1",
                "admin",
                approvedMinutes:
                    90,
                note:
                    "Duyệt một phần");

        await database.Persistence.ApplyAsync(
            statusChange,
            "user-1",
            "admin");

        await using HrManagementDbContext dbContext =
            await database.Factory.CreateDbContextAsync();

        OvertimeRequest saved =
            await dbContext
                .OvertimeRequests
                .AsNoTracking()
                .SingleAsync();

        Assert.Equal(
            OvertimeRequestStatus.Approved,
            saved.Status);

        Assert.Equal(
            90,
            saved.ApprovedMinutes);

        OvertimeRequestStatusChange history =
            await dbContext
                .OvertimeRequestStatusChanges
                .AsNoTracking()
                .SingleAsync();

        Assert.Equal(
            OvertimeRequestStatus.Pending,
            history.PreviousStatus);

        Assert.Equal(
            OvertimeRequestStatus.Approved,
            history.NewStatus);

        Assert.Equal(
            90,
            history.ApprovedMinutes);

        Assert.Equal(
            "Duyệt một phần",
            history.Note);

        AuditEntry audit =
            await dbContext
                .AuditEntries
                .AsNoTracking()
                .SingleAsync();

        Assert.Equal(
            AuditAction.Updated,
            audit.Action);

        Assert.Equal(
            AuditEntityTypes.OvertimeRequest,
            audit.EntityType);

        Assert.Equal(
            seed.Request.Id,
            audit.EntityId);

        Assert.Equal(
            seed.EmployeeId,
            audit.EmployeeId);

        Assert.Equal(
            "user-1",
            audit.ActorUserId);

        Assert.Equal(
            "admin",
            audit.ActorUsername);
    }

    [Fact]
    public async Task ApplyAsync_WhenPreviousStatusIsStale_RejectsWithoutHistoryOrAudit()
    {
        await using TestDatabase database =
            await TestDatabase.CreateAsync();

        SeedResult seed =
            await database.SeedPendingRequestAsync();

        OvertimeRequestStatusChange staleChange =
            seed.Request.TransitionTo(
                Guid.NewGuid(),
                OvertimeRequestStatus.Approved,
                Utc(
                    14),
                "user-1",
                "admin",
                approvedMinutes:
                    120);

        await using (
            HrManagementDbContext dbContext =
                await database.Factory.CreateDbContextAsync())
        {
            await dbContext
                .OvertimeRequests
                .Where(
                    request =>
                        request.Id ==
                        seed.Request.Id)
                .ExecuteUpdateAsync(
                    setters =>
                        setters.SetProperty(
                            request =>
                                request.Status,
                            OvertimeRequestStatus.Rejected));
        }

        OvertimeRequestStatusConcurrencyException exception =
            await Assert.ThrowsAsync<
                OvertimeRequestStatusConcurrencyException>(
                    () =>
                        database.Persistence.ApplyAsync(
                            staleChange,
                            "user-1",
                            "admin"));

        Assert.Equal(
            "Yêu cầu tăng ca đã thay đổi trạng thái. Vui lòng làm mới dữ liệu trước khi thao tác.",
            exception.Message);

        await using HrManagementDbContext verification =
            await database.Factory.CreateDbContextAsync();

        OvertimeRequest saved =
            await verification
                .OvertimeRequests
                .AsNoTracking()
                .SingleAsync();

        Assert.Equal(
            OvertimeRequestStatus.Rejected,
            saved.Status);

        Assert.Empty(
            await verification
                .OvertimeRequestStatusChanges
                .AsNoTracking()
                .ToArrayAsync());

        Assert.Empty(
            await verification
                .AuditEntries
                .AsNoTracking()
                .ToArrayAsync());
    }

    [Fact]
    public async Task ApplyAsync_WhenTimesheetPeriodIsClosed_AllowsExistingRequestApproval()
    {
        await using TestDatabase database =
            await TestDatabase.CreateAsync();

        SeedResult seed =
            await database.SeedPendingRequestAsync();

        await database.ClosePeriodAsync(
            seed.Request.WorkDate);

        OvertimeRequestStatusChange statusChange =
            seed.Request.TransitionTo(
                Guid.NewGuid(),
                OvertimeRequestStatus.Approved,
                Utc(
                    14),
                "user-1",
                "admin",
                approvedMinutes:
                    120);

        await database.Persistence.ApplyAsync(
            statusChange,
            "user-1",
            "admin");

        await using HrManagementDbContext verification =
            await database.Factory.CreateDbContextAsync();

        OvertimeRequest saved =
            await verification
                .OvertimeRequests
                .AsNoTracking()
                .SingleAsync();

        Assert.Equal(
            OvertimeRequestStatus.Approved,
            saved.Status);

        Assert.Equal(
            120,
            saved.ApprovedMinutes);

        OvertimeRequestStatusChange history =
            await verification
                .OvertimeRequestStatusChanges
                .AsNoTracking()
                .SingleAsync();

        Assert.Equal(
            OvertimeRequestStatus.Pending,
            history.PreviousStatus);

        Assert.Equal(
            OvertimeRequestStatus.Approved,
            history.NewStatus);

        AuditEntry audit =
            await verification
                .AuditEntries
                .AsNoTracking()
                .SingleAsync();

        Assert.Equal(
            AuditAction.Updated,
            audit.Action);

        Assert.Equal(
            seed.Request.Id,
            audit.EntityId);
    }

    [Fact]
    public async Task ApplyAsync_CancelApprovedRequest_ClearsApprovedMinutes()
    {
        await using TestDatabase database =
            await TestDatabase.CreateAsync();

        SeedResult seed =
            await database.SeedApprovedRequestAsync();

        OvertimeRequestStatusChange cancellation =
            seed.Request.TransitionTo(
                Guid.NewGuid(),
                OvertimeRequestStatus.Cancelled,
                Utc(
                    15),
                "user-1",
                "admin");

        await database.Persistence.ApplyAsync(
            cancellation,
            "user-1",
            "admin");

        await using HrManagementDbContext verification =
            await database.Factory.CreateDbContextAsync();

        OvertimeRequest saved =
            await verification
                .OvertimeRequests
                .AsNoTracking()
                .SingleAsync();

        Assert.Equal(
            OvertimeRequestStatus.Cancelled,
            saved.Status);

        Assert.Null(
            saved.ApprovedMinutes);

        OvertimeRequestStatusChange history =
            await verification
                .OvertimeRequestStatusChanges
                .AsNoTracking()
                .SingleAsync();

        Assert.Equal(
            OvertimeRequestStatus.Approved,
            history.PreviousStatus);

        Assert.Equal(
            OvertimeRequestStatus.Cancelled,
            history.NewStatus);
    }

    [Fact]
    public async Task ApplyAsync_WhenAuditActorDoesNotMatch_RejectsWithoutWrites()
    {
        await using TestDatabase database =
            await TestDatabase.CreateAsync(
                auditActorUserId:
                    "other-user");

        SeedResult seed =
            await database.SeedPendingRequestAsync();

        OvertimeRequestStatusChange statusChange =
            seed.Request.TransitionTo(
                Guid.NewGuid(),
                OvertimeRequestStatus.Rejected,
                Utc(
                    14),
                "user-1",
                "admin");

        InvalidOperationException exception =
            await Assert.ThrowsAsync<InvalidOperationException>(
                () =>
                    database.Persistence.ApplyAsync(
                        statusChange,
                        "user-1",
                        "admin"));

        Assert.Equal(
            "Người thay đổi trạng thái tăng ca không khớp với người dùng audit hiện tại.",
            exception.Message);

        await using HrManagementDbContext verification =
            await database.Factory.CreateDbContextAsync();

        OvertimeRequest saved =
            await verification
                .OvertimeRequests
                .AsNoTracking()
                .SingleAsync();

        Assert.Equal(
            OvertimeRequestStatus.Pending,
            saved.Status);

        Assert.Empty(
            await verification
                .OvertimeRequestStatusChanges
                .AsNoTracking()
                .ToArrayAsync());

        Assert.Empty(
            await verification
                .AuditEntries
                .AsNoTracking()
                .ToArrayAsync());
    }

    private sealed record SeedResult(
        Guid EmployeeId,
        OvertimeRequest Request);

    private sealed class TestDatabase
        : IAsyncDisposable
    {
        private readonly SqliteConnection
            _connection;

        public TestDbContextFactory Factory
        {
            get;
        }

        public EfOvertimeRequestStatusTransitionPersistence Persistence
        {
            get;
        }

        private TestDatabase(
            SqliteConnection connection,
            TestDbContextFactory factory,
            EfOvertimeRequestStatusTransitionPersistence persistence)
        {
            _connection =
                connection;

            Factory =
                factory;

            Persistence =
                persistence;
        }

        public static async Task<TestDatabase> CreateAsync(
            string auditActorUserId = "user-1")
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
                    await factory.CreateDbContextAsync())
            {
                await dbContext.Database
                    .EnsureCreatedAsync();
            }

            var auditFactory =
                new StubAuditEntryFactory(
                    auditActorUserId,
                    "admin");

            var persistence =
                new EfOvertimeRequestStatusTransitionPersistence(
                    factory,
                    auditFactory);

            return new TestDatabase(
                connection,
                factory,
                persistence);
        }

        public Task<SeedResult> SeedPendingRequestAsync()
        {
            return SeedRequestAsync(
                approved:
                    false);
        }

        public Task<SeedResult> SeedApprovedRequestAsync()
        {
            return SeedRequestAsync(
                approved:
                    true);
        }

        private async Task<SeedResult> SeedRequestAsync(
            bool approved)
        {
            Guid employeeId =
                Guid.NewGuid();

            Guid employmentPeriodId =
                Guid.NewGuid();

            var employee =
                new Employee(
                    employeeId,
                    "EMP001",
                    "Nguyễn Văn An",
                    email: null,
                    phoneNumber: null,
                    dateOfBirth: null,
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

            var employmentPeriod =
                new EmploymentPeriod(
                    employmentPeriodId,
                    employeeId,
                    new DateOnly(
                        2026,
                        1,
                        1));

            var request =
                new OvertimeRequest(
                    Guid.NewGuid(),
                    employeeId,
                    employmentPeriodId,
                    new DateOnly(
                        2026,
                        8,
                        27),
                    120,
                    "Kiểm thử tăng ca",
                    Utc(
                        11));

            if (approved)
            {
                request.TransitionTo(
                    Guid.NewGuid(),
                    OvertimeRequestStatus.Approved,
                    Utc(
                        12),
                    "user-1",
                    "admin",
                    approvedMinutes:
                        120);
            }

            await using HrManagementDbContext dbContext =
                await Factory.CreateDbContextAsync();

            dbContext.Employees.Add(
                employee);

            dbContext.EmploymentPeriods.Add(
                employmentPeriod);

            dbContext.OvertimeRequests.Add(
                request);

            await dbContext.SaveChangesAsync();

            return new SeedResult(
                employeeId,
                request);
        }

        public async Task ClosePeriodAsync(
            DateOnly workDate)
        {
            var period =
                new HrManagement.Domain.Attendance.Timesheets.TimesheetPeriod(
                    Guid.NewGuid(),
                    workDate.Year,
                    workDate.Month);

            period.Close(
                Utc(
                    18),
                "user-1",
                "admin");

            await using HrManagementDbContext dbContext =
                await Factory.CreateDbContextAsync();

            dbContext.TimesheetPeriods.Add(
                period);

            await dbContext.SaveChangesAsync();
        }

        public async ValueTask DisposeAsync()
        {
            await _connection.DisposeAsync();
        }
    }

    private sealed class StubAuditEntryFactory
        : IAuditEntryFactory
    {
        private readonly string
            _actorUserId;

        private readonly string
            _actorUsername;

        public StubAuditEntryFactory(
            string actorUserId,
            string actorUsername)
        {
            _actorUserId =
                actorUserId;

            _actorUsername =
                actorUsername;
        }

        public AuditEntry Create(
            AuditAction action,
            string entityType,
            Guid entityId,
            Guid? employeeId = null)
        {
            return new AuditEntry(
                Guid.NewGuid(),
                Utc(
                    16),
                _actorUserId,
                _actorUsername,
                action,
                entityType,
                entityId,
                employeeId);
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
            27,
            hour,
            0,
            0,
            DateTimeKind.Utc);
    }
}
