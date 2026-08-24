using HrManagement.Application.Attendance.Corrections;
using HrManagement.Application.Auditing;
using HrManagement.Domain.Attendance.Corrections;
using HrManagement.Domain.Attendance.Records;
using HrManagement.Domain.Auditing;
using HrManagement.Infrastructure.Attendance.Corrections;
using HrManagement.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace HrManagement.Tests.Attendance;

public sealed class AttendanceCorrectionPersistenceTests
{
    [Fact]
    public async Task GetByAttendanceRecordIdAsync_ReturnsCorrectionsInRevisionOrder()
    {
        await using TestDatabase database =
            await TestDatabase.CreateAsync();

        SeedResult seed =
            await database.SeedAttendanceRecordAsync();

        AttendanceCorrection revision2 =
            CreateCorrection(
                seed,
                revision: 2,
                affectedEventId:
                    Guid.NewGuid());

        AttendanceCorrection revision1 =
            CreateCorrection(
                seed,
                revision: 1,
                affectedEventId:
                    Guid.NewGuid());

        await using (
            HrManagementDbContext dbContext =
                await database.Factory
                    .CreateDbContextAsync())
        {
            dbContext.AttendanceCorrections.AddRange(
                revision2,
                revision1);

            await dbContext.SaveChangesAsync();
        }

        IReadOnlyList<AttendanceCorrection> result =
            await database.Persistence
                .GetByAttendanceRecordIdAsync(
                    seed.AttendanceRecordId);

        Assert.Equal(
            2,
            result.Count);

        Assert.Equal(
            1,
            result[0].Revision);

        Assert.Equal(
            2,
            result[1].Revision);
    }

    [Fact]
    public async Task AppendAsync_PersistsCorrectionAndMatchingAuditEntry()
    {
        Guid auditId =
            Guid.NewGuid();

        var auditFactory =
            new StubAuditEntryFactory(
                auditId,
                "user-1",
                "admin");

        await using TestDatabase database =
            await TestDatabase.CreateAsync(
                auditFactory);

        SeedResult seed =
            await database.SeedAttendanceRecordAsync();

        AttendanceCorrection correction =
            CreateCorrection(
                seed,
                revision: 1,
                affectedEventId:
                    Guid.NewGuid());

        await database.Persistence
            .AppendAsync(
                correction);

        await using HrManagementDbContext dbContext =
            await database.Factory
                .CreateDbContextAsync();

        AttendanceCorrection persistedCorrection =
            await dbContext
                .AttendanceCorrections
                .AsNoTracking()
                .SingleAsync();

        Assert.Equal(
            correction.Id,
            persistedCorrection.Id);

        Assert.Equal(
            seed.AttendanceRecordId,
            persistedCorrection.AttendanceRecordId);

        Assert.Equal(
            seed.EmployeeId,
            persistedCorrection.EmployeeId);

        AuditEntry auditEntry =
            await dbContext
                .AuditEntries
                .AsNoTracking()
                .SingleAsync();

        Assert.Equal(
            auditId,
            auditEntry.Id);

        Assert.Equal(
            AuditAction.Created,
            auditEntry.Action);

        Assert.Equal(
            AuditEntityTypes.AttendanceCorrection,
            auditEntry.EntityType);

        Assert.Equal(
            correction.Id,
            auditEntry.EntityId);

        Assert.Equal(
            seed.EmployeeId,
            auditEntry.EmployeeId);

        Assert.Equal(
            correction.ActorUserId,
            auditEntry.ActorUserId);

        Assert.Equal(
            correction.ActorUsername,
            auditEntry.ActorUsername);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(3)]
    public async Task AppendAsync_WhenRevisionIsNotNext_RejectsWriteAndAudit(
    int invalidRevision)
    {
        var auditFactory =
            new StubAuditEntryFactory(
                Guid.NewGuid(),
                "user-1",
                "admin");

        await using TestDatabase database =
            await TestDatabase.CreateAsync(
                auditFactory);

        SeedResult seed =
            await database.SeedAttendanceRecordAsync();

        AttendanceCorrection existingCorrection =
            CreateCorrection(
                seed,
                revision: 1,
                affectedEventId:
                    Guid.NewGuid());

        await using (
            HrManagementDbContext dbContext =
                await database.Factory
                    .CreateDbContextAsync())
        {
            dbContext.AttendanceCorrections.Add(
                existingCorrection);

            await dbContext.SaveChangesAsync();
        }

        AttendanceCorrection invalidCorrection =
            CreateCorrection(
                seed,
                revision:
                    invalidRevision,
                affectedEventId:
                    Guid.NewGuid());

        InvalidOperationException exception =
            await Assert.ThrowsAsync<InvalidOperationException>(
                () =>
                    database.Persistence
                        .AppendAsync(
                            invalidCorrection));

        Assert.Equal(
            "Phiên bản điều chỉnh chấm công phải là phiên bản kế tiếp.",
            exception.Message);

        await using HrManagementDbContext verificationContext =
            await database.Factory
                .CreateDbContextAsync();

        AttendanceCorrection[] corrections =
            await verificationContext
                .AttendanceCorrections
                .AsNoTracking()
                .ToArrayAsync();

        Assert.Single(
            corrections);

        Assert.Equal(
            1,
            corrections[0].Revision);

        Assert.Empty(
            await verificationContext
                .AuditEntries
                .AsNoTracking()
                .ToArrayAsync());
    }

    [Fact]
    public async Task AppendAsync_WhenFirstRevisionIsNotOne_RejectsWriteAndAudit()
    {
        var auditFactory =
            new StubAuditEntryFactory(
                Guid.NewGuid(),
                "user-1",
                "admin");

        await using TestDatabase database =
            await TestDatabase.CreateAsync(
                auditFactory);

        SeedResult seed =
            await database.SeedAttendanceRecordAsync();

        AttendanceCorrection correction =
            CreateCorrection(
                seed,
                revision: 2,
                affectedEventId:
                    Guid.NewGuid());

        InvalidOperationException exception =
            await Assert.ThrowsAsync<InvalidOperationException>(
                () =>
                    database.Persistence
                        .AppendAsync(
                            correction));

        Assert.Equal(
            "Phiên bản điều chỉnh chấm công phải là phiên bản kế tiếp.",
            exception.Message);

        await using HrManagementDbContext dbContext =
            await database.Factory
                .CreateDbContextAsync();

        Assert.Empty(
            await dbContext
                .AttendanceCorrections
                .AsNoTracking()
                .ToArrayAsync());

        Assert.Empty(
            await dbContext
                .AuditEntries
                .AsNoTracking()
                .ToArrayAsync());
    }

    [Fact]
    public async Task AppendAsync_WhenAuditInsertFails_DoesNotPersistCorrection()
    {
        Guid duplicatedAuditId =
            Guid.NewGuid();

        var auditFactory =
            new StubAuditEntryFactory(
                duplicatedAuditId,
                "user-1",
                "admin");

        await using TestDatabase database =
            await TestDatabase.CreateAsync(
                auditFactory);

        SeedResult seed =
            await database.SeedAttendanceRecordAsync();

        await using (
            HrManagementDbContext dbContext =
                await database.Factory
                    .CreateDbContextAsync())
        {
            dbContext.AuditEntries.Add(
                new AuditEntry(
                    duplicatedAuditId,
                    Utc(
                        11),
                    "existing-user",
                    "existing",
                    AuditAction.Created,
                    "ExistingEntity",
                    Guid.NewGuid(),
                    seed.EmployeeId));

            await dbContext.SaveChangesAsync();
        }

        AttendanceCorrection correction =
            CreateCorrection(
                seed,
                revision: 1,
                affectedEventId:
                    Guid.NewGuid());

        await Assert.ThrowsAsync<DbUpdateException>(
            () =>
                database.Persistence
                    .AppendAsync(
                        correction));

        await using HrManagementDbContext verificationContext =
            await database.Factory
                .CreateDbContextAsync();

        Assert.Empty(
            await verificationContext
                .AttendanceCorrections
                .AsNoTracking()
                .ToArrayAsync());

        Assert.Single(
            await verificationContext
                .AuditEntries
                .AsNoTracking()
                .ToArrayAsync());
    }

    [Fact]
    public async Task AppendAsync_WithDifferentAuditActor_RejectsWrite()
    {
        var auditFactory =
            new StubAuditEntryFactory(
                Guid.NewGuid(),
                "other-user",
                "other-admin");

        await using TestDatabase database =
            await TestDatabase.CreateAsync(
                auditFactory);

        SeedResult seed =
            await database.SeedAttendanceRecordAsync();

        AttendanceCorrection correction =
            CreateCorrection(
                seed,
                revision: 1,
                affectedEventId:
                    Guid.NewGuid());

        InvalidOperationException exception =
            await Assert.ThrowsAsync<InvalidOperationException>(
                () =>
                    database.Persistence
                        .AppendAsync(
                            correction));

        Assert.Equal(
            "Người thực hiện correction không khớp với người dùng audit hiện tại.",
            exception.Message);

        await using HrManagementDbContext dbContext =
            await database.Factory
                .CreateDbContextAsync();

        Assert.Empty(
            await dbContext
                .AttendanceCorrections
                .AsNoTracking()
                .ToArrayAsync());

        Assert.Empty(
            await dbContext
                .AuditEntries
                .AsNoTracking()
                .ToArrayAsync());
    }

    [Fact]
    public async Task AppendAsync_WhenAttendanceRecordDoesNotExist_RejectsWriteAndAudit()
    {
        var auditFactory =
            new StubAuditEntryFactory(
                Guid.NewGuid(),
                "user-1",
                "admin");

        await using TestDatabase database =
            await TestDatabase.CreateAsync(
                auditFactory);

        var seed =
            new SeedResult(
                Guid.NewGuid(),
                Guid.NewGuid());

        AttendanceCorrection correction =
            CreateCorrection(
                seed,
                revision: 1,
                affectedEventId:
                    Guid.NewGuid());

        InvalidOperationException exception =
            await Assert.ThrowsAsync<InvalidOperationException>(
                () =>
                    database.Persistence
                        .AppendAsync(
                            correction));

        Assert.Equal(
            "Không tìm thấy bản ghi chấm công cần điều chỉnh.",
            exception.Message);

        await using HrManagementDbContext dbContext =
            await database.Factory
                .CreateDbContextAsync();

        Assert.Empty(
            await dbContext
                .AttendanceCorrections
                .AsNoTracking()
                .ToArrayAsync());

        Assert.Empty(
            await dbContext
                .AuditEntries
                .AsNoTracking()
                .ToArrayAsync());
    }

    [Fact]
    public async Task AppendAsync_WhenEmployeeDoesNotMatchAttendanceRecord_RejectsWriteAndAudit()
    {
        var auditFactory =
            new StubAuditEntryFactory(
                Guid.NewGuid(),
                "user-1",
                "admin");

        await using TestDatabase database =
            await TestDatabase.CreateAsync(
                auditFactory);

        SeedResult seed =
            await database.SeedAttendanceRecordAsync();

        var mismatchedSeed =
            new SeedResult(
                seed.AttendanceRecordId,
                Guid.NewGuid());

        AttendanceCorrection correction =
            CreateCorrection(
                mismatchedSeed,
                revision: 1,
                affectedEventId:
                    Guid.NewGuid());

        InvalidOperationException exception =
            await Assert.ThrowsAsync<InvalidOperationException>(
                () =>
                    database.Persistence
                        .AppendAsync(
                            correction));

        Assert.Equal(
            "Điều chỉnh chấm công không thuộc nhân viên của bản ghi chấm công.",
            exception.Message);

        await using HrManagementDbContext dbContext =
            await database.Factory
                .CreateDbContextAsync();

        Assert.Empty(
            await dbContext
                .AttendanceCorrections
                .AsNoTracking()
                .ToArrayAsync());

        Assert.Empty(
            await dbContext
                .AuditEntries
                .AsNoTracking()
                .ToArrayAsync());
    }

    [Fact]
    public async Task AppendAsync_WhenRevisionIsNext_PersistsCorrectionAndAudit()
    {
        var auditFactory =
            new SequentialStubAuditEntryFactory(
                "user-1",
                "admin");

        await using TestDatabase database =
            await TestDatabase.CreateAsync(
                auditFactory);

        SeedResult seed =
            await database.SeedAttendanceRecordAsync();

        AttendanceCorrection revision1 =
            CreateCorrection(
                seed,
                revision: 1,
                affectedEventId:
                    Guid.NewGuid());

        AttendanceCorrection revision2 =
            CreateCorrection(
                seed,
                revision: 2,
                affectedEventId:
                    Guid.NewGuid());

        await database.Persistence
            .AppendAsync(
                revision1);

        await database.Persistence
            .AppendAsync(
                revision2);

        await using HrManagementDbContext dbContext =
            await database.Factory
                .CreateDbContextAsync();

        AttendanceCorrection[] corrections =
            await dbContext
                .AttendanceCorrections
                .AsNoTracking()
                .OrderBy(
                    correction =>
                        correction.Revision)
                .ToArrayAsync();

        Assert.Equal(
            2,
            corrections.Length);

        Assert.Equal(
            1,
            corrections[0].Revision);

        Assert.Equal(
            2,
            corrections[1].Revision);

        AuditEntry[] audits =
            await dbContext
                .AuditEntries
                .AsNoTracking()
                .OrderBy(
                    audit =>
                        audit.OccurredAtUtc)
                .ToArrayAsync();

        Assert.Equal(
            2,
            audits.Length);

        Assert.All(
            audits,
            audit =>
            {
                Assert.Equal(
                    AuditAction.Created,
                    audit.Action);

                Assert.Equal(
                    AuditEntityTypes.AttendanceCorrection,
                    audit.EntityType);

                Assert.Equal(
                    seed.EmployeeId,
                    audit.EmployeeId);
            });

        Assert.Contains(
            audits,
            audit =>
                audit.EntityId ==
                revision1.Id);

        Assert.Contains(
            audits,
            audit =>
                audit.EntityId ==
                revision2.Id);
    }


    private static AttendanceCorrection CreateCorrection(
        SeedResult seed,
        int revision,
        Guid affectedEventId)
    {
        return new AttendanceCorrection(
            Guid.NewGuid(),
            seed.AttendanceRecordId,
            seed.EmployeeId,
            affectedEventId,
            revision,
            AttendanceCorrectionKind.AddEvent,
            null,
            null,
            AttendanceEventType.ClockIn,
            Utc(
                8),
            "Bổ sung chấm vào",
            Utc(
                12).AddMinutes(
                    revision),
            "user-1",
            "admin");
    }

    private static DateTime Utc(
        int hour,
        int minute = 0)
    {
        return new DateTime(
            2026,
            8,
            24,
            hour,
            minute,
            0,
            DateTimeKind.Utc);
    }

    private sealed record SeedResult(
        Guid AttendanceRecordId,
        Guid EmployeeId);

    private sealed class SequentialStubAuditEntryFactory
    : IAuditEntryFactory
    {
        private readonly string
            _actorUserId;

        private readonly string
            _actorUsername;

        private int
            _sequence;

        public SequentialStubAuditEntryFactory(
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
            _sequence++;

            return new AuditEntry(
                Guid.NewGuid(),
                Utc(
                    13).AddMinutes(
                        _sequence),
                _actorUserId,
                _actorUsername,
                action,
                entityType,
                entityId,
                employeeId);
        }
    }

    private sealed class StubAuditEntryFactory
        : IAuditEntryFactory
    {
        private readonly Guid
            _auditId;

        private readonly string
            _actorUserId;

        private readonly string
            _actorUsername;

        public StubAuditEntryFactory(
            Guid auditId,
            string actorUserId,
            string actorUsername)
        {
            _auditId =
                auditId;

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
                _auditId,
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

    private sealed class TestDatabase
        : IAsyncDisposable
    {
        private readonly SqliteConnection
            _connection;

        public TestDbContextFactory Factory
        {
            get;
        }

        public EfAttendanceCorrectionPersistence Persistence
        {
            get;
        }

        private TestDatabase(
            SqliteConnection connection,
            TestDbContextFactory factory,
            IAuditEntryFactory auditEntryFactory)
        {
            _connection =
                connection;

            Factory =
                factory;

            Persistence =
                new EfAttendanceCorrectionPersistence(
                    factory,
                    auditEntryFactory);
        }

        public static async Task<TestDatabase> CreateAsync(
            IAuditEntryFactory? auditEntryFactory = null)
        {
            var connection =
                new SqliteConnection(
                    "Data Source=:memory:;Foreign Keys=False");

            await connection.OpenAsync();

            DbContextOptions<HrManagementDbContext> options =
                new DbContextOptionsBuilder<HrManagementDbContext>()
                    .UseSqlite(
                        connection)
                    .Options;

            var factory =
                new TestDbContextFactory(
                    options);

            await using HrManagementDbContext dbContext =
                await factory
                    .CreateDbContextAsync();

            await dbContext.Database
                .EnsureCreatedAsync();

            return new TestDatabase(
                connection,
                factory,
                auditEntryFactory
                    ?? new StubAuditEntryFactory(
                        Guid.NewGuid(),
                        "user-1",
                        "admin"));
        }

        public async Task<SeedResult>
            SeedAttendanceRecordAsync()
        {
            Guid attendanceRecordId =
                Guid.NewGuid();

            Guid employeeId =
                Guid.NewGuid();

            var record =
                new AttendanceRecord(
                    attendanceRecordId,
                    employeeId,
                    Guid.NewGuid(),
                    Guid.NewGuid(),
                    Guid.NewGuid(),
                    new DateOnly(
                        2026,
                        8,
                        24),
                    "SE Asia Standard Time",
                    false);

            await using HrManagementDbContext dbContext =
                await Factory
                    .CreateDbContextAsync();

            dbContext.AttendanceRecords.Add(
                record);

            await dbContext.SaveChangesAsync();

            return new SeedResult(
                attendanceRecordId,
                employeeId);
        }

        public async ValueTask DisposeAsync()
        {
            await _connection.DisposeAsync();
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
}
