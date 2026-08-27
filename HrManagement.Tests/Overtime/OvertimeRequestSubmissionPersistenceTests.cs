using HrManagement.Application.Auditing;
using HrManagement.Domain.Auditing;
using HrManagement.Domain.Employees;
using HrManagement.Domain.Overtime.Requests;
using HrManagement.Infrastructure.Overtime.Requests;
using HrManagement.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace HrManagement.Tests.Overtime;

public sealed class OvertimeRequestSubmissionPersistenceTests
{
    [Fact]
    public async Task ContextSource_ReturnsEmploymentPeriodCoveringWorkDate()
    {
        await using TestDatabase database =
            await TestDatabase.CreateAsync();

        SeedResult seed =
            await database.SeedEmployeeAsync();

        var source =
            new EfOvertimeRequestSubmissionContextSource(
                database.Factory);

        EmploymentPeriod? result =
            await source.GetEmploymentPeriodAsync(
                seed.EmployeeId,
                seed.WorkDate);

        Assert.NotNull(
            result);

        Assert.Equal(
            seed.EmploymentPeriodId,
            result.Id);
    }

    [Fact]
    public async Task SubmitAsync_WhenValid_PersistsRequestAndMetadataAudit()
    {
        await using TestDatabase database =
            await TestDatabase.CreateAsync();

        SeedResult seed =
            await database.SeedEmployeeAsync();

        OvertimeRequest request =
            CreateRequest(
                seed);

        await database.Persistence.SubmitAsync(
            request,
            "user-1",
            "admin");

        await using HrManagementDbContext dbContext =
            await database.Factory
                .CreateDbContextAsync();

        OvertimeRequest savedRequest =
            await dbContext
                .OvertimeRequests
                .AsNoTracking()
                .SingleAsync();

        Assert.Equal(
            request.Id,
            savedRequest.Id);

        Assert.Equal(
            OvertimeRequestStatus.Pending,
            savedRequest.Status);

        AuditEntry audit =
            await dbContext
                .AuditEntries
                .AsNoTracking()
                .SingleAsync();

        Assert.Equal(
            AuditAction.Created,
            audit.Action);

        Assert.Equal(
            AuditEntityTypes.OvertimeRequest,
            audit.EntityType);

        Assert.Equal(
            request.Id,
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
    public async Task SubmitAsync_WhenActiveRequestAlreadyExists_RejectsWithoutSecondAudit()
    {
        await using TestDatabase database =
            await TestDatabase.CreateAsync();

        SeedResult seed =
            await database.SeedEmployeeAsync();

        await database.Persistence.SubmitAsync(
            CreateRequest(
                seed),
            "user-1",
            "admin");

        InvalidOperationException exception =
            await Assert.ThrowsAsync<InvalidOperationException>(
                () =>
                    database.Persistence.SubmitAsync(
                        CreateRequest(
                            seed),
                        "user-1",
                        "admin"));

        Assert.Equal(
            "Nhân viên đã có một yêu cầu tăng ca đang có hiệu lực trong ngày này.",
            exception.Message);

        await using HrManagementDbContext dbContext =
            await database.Factory
                .CreateDbContextAsync();

        Assert.Single(
            await dbContext
                .OvertimeRequests
                .AsNoTracking()
                .ToArrayAsync());

        Assert.Single(
            await dbContext
                .AuditEntries
                .AsNoTracking()
                .ToArrayAsync());
    }

    [Fact]
    public async Task SubmitAsync_WhenPeriodIsClosed_RejectsWithoutWrites()
    {
        await using TestDatabase database =
            await TestDatabase.CreateAsync();

        SeedResult seed =
            await database.SeedEmployeeAsync();

        await database.ClosePeriodAsync(
            seed.WorkDate);

        InvalidOperationException exception =
            await Assert.ThrowsAsync<InvalidOperationException>(
                () =>
                    database.Persistence.SubmitAsync(
                        CreateRequest(
                            seed),
                        "user-1",
                        "admin"));

        Assert.Equal(
            "Kỳ công của ngày tăng ca đã được đóng. Không thể gửi yêu cầu tăng ca.",
            exception.Message);

        await using HrManagementDbContext dbContext =
            await database.Factory
                .CreateDbContextAsync();

        Assert.Empty(
            await dbContext
                .OvertimeRequests
                .AsNoTracking()
                .ToArrayAsync());

        Assert.Empty(
            await dbContext
                .AuditEntries
                .AsNoTracking()
                .ToArrayAsync());
    }

    [Fact]
    public async Task SubmitAsync_WhenAuditActorDoesNotMatch_RejectsWithoutWrites()
    {
        await using TestDatabase database =
            await TestDatabase.CreateAsync(
                auditActorUserId:
                    "other-user");

        SeedResult seed =
            await database.SeedEmployeeAsync();

        InvalidOperationException exception =
            await Assert.ThrowsAsync<InvalidOperationException>(
                () =>
                    database.Persistence.SubmitAsync(
                        CreateRequest(
                            seed),
                        "user-1",
                        "admin"));

        Assert.Equal(
            "Người gửi yêu cầu tăng ca không khớp với người dùng audit hiện tại.",
            exception.Message);

        await using HrManagementDbContext dbContext =
            await database.Factory
                .CreateDbContextAsync();

        Assert.Empty(
            await dbContext
                .OvertimeRequests
                .AsNoTracking()
                .ToArrayAsync());

        Assert.Empty(
            await dbContext
                .AuditEntries
                .AsNoTracking()
                .ToArrayAsync());
    }

    private static OvertimeRequest CreateRequest(
        SeedResult seed)
    {
        return new OvertimeRequest(
            Guid.NewGuid(),
            seed.EmployeeId,
            seed.EmploymentPeriodId,
            seed.WorkDate,
            120,
            "Kiểm thử tăng ca",
            Utc(
                12));
    }

    private sealed record SeedResult(
        Guid EmployeeId,
        Guid EmploymentPeriodId,
        DateOnly WorkDate);

    private sealed class TestDatabase
        : IAsyncDisposable
    {
        private readonly SqliteConnection
            _connection;

        public TestDbContextFactory Factory
        {
            get;
        }

        public EfOvertimeRequestSubmissionPersistence Persistence
        {
            get;
        }

        private TestDatabase(
            SqliteConnection connection,
            TestDbContextFactory factory,
            EfOvertimeRequestSubmissionPersistence persistence)
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
                new EfOvertimeRequestSubmissionPersistence(
                    factory,
                    auditFactory);

            return new TestDatabase(
                connection,
                factory,
                persistence);
        }

        public async Task<SeedResult> SeedEmployeeAsync()
        {
            Guid employeeId =
                Guid.NewGuid();

            Guid employmentPeriodId =
                Guid.NewGuid();

            DateOnly workDate =
                new(
                    2026,
                    8,
                    27);

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

            await using HrManagementDbContext dbContext =
                await Factory
                    .CreateDbContextAsync();

            dbContext.Employees.Add(
                employee);

            dbContext.EmploymentPeriods.Add(
                employmentPeriod);

            await dbContext.SaveChangesAsync();

            return new SeedResult(
                employeeId,
                employmentPeriodId,
                workDate);
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
                await Factory
                    .CreateDbContextAsync();

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
                    13),
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
