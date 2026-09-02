using HrManagement.Application.Auditing;
using HrManagement.Application.Authentication;
using HrManagement.Application.Payroll.Compensation;
using HrManagement.Domain.Auditing;
using HrManagement.Domain.Employees;
using HrManagement.Domain.Payroll.Compensation;
using HrManagement.Infrastructure.Payroll.Compensation;
using HrManagement.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace HrManagement.Tests.Payroll;

public sealed class EmployeeCompensationPersistenceTests
{
    [Fact]
    public async Task ContextSource_ReturnsEmploymentPeriodAndOpenCompensation()
    {
        await using TestDatabase database =
            await TestDatabase.CreateAsync();

        SeedResult seed =
            await database.SeedAsync(
                withOpenCompensation:
                    true);

        EmployeeCompensationContext? context =
            await database.ContextSource.GetAsync(
                seed.EmployeeId,
                new DateOnly(
                    2026,
                    9,
                    1));

        Assert.NotNull(
            context);

        Assert.Equal(
            seed.EmploymentPeriodId,
            context!.EmploymentPeriod.Id);

        Assert.NotNull(
            context.CurrentCompensation);

        Assert.Equal(
            seed.CompensationId,
            context.CurrentCompensation!.Id);
    }

    [Fact]
    public async Task ApplyAsync_WhenInitialCompensation_PersistsWithCreatedAudit()
    {
        await using TestDatabase database =
            await TestDatabase.CreateAsync();

        SeedResult seed =
            await database.SeedAsync(
                withOpenCompensation:
                    false);

        var compensation =
            new EmployeeCompensation(
                Guid.NewGuid(),
                seed.EmployeeId,
                seed.EmploymentPeriodId,
                new DateOnly(
                    2026,
                    8,
                    1),
                25_000_000m,
                "VND");

        await database.Persistence.ApplyAsync(
            null,
            compensation,
            "user-1",
            "admin");

        await using HrManagementDbContext verification =
            database.CreateContext();

        EmployeeCompensation saved =
            await verification
                .EmployeeCompensations
                .AsNoTracking()
                .SingleAsync();

        Assert.Equal(
            compensation.Id,
            saved.Id);

        Assert.Equal(
            25_000_000m,
            saved.MonthlyBaseSalary);

        AuditEntry audit =
            await verification
                .AuditEntries
                .AsNoTracking()
                .SingleAsync();

        Assert.Equal(
            AuditAction.Created,
            audit.Action);

        Assert.Equal(
            AuditEntityTypes.EmployeeCompensation,
            audit.EntityType);

        Assert.Equal(
            compensation.Id,
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
    public async Task ApplyAsync_WhenRevision_ClosesOldCreatesNewAndWritesMetadataAudits()
    {
        await using TestDatabase database =
            await TestDatabase.CreateAsync();

        SeedResult seed =
            await database.SeedAsync(
                withOpenCompensation:
                    true);

        EmployeeCompensationContext context =
            await database.ContextSource.GetAsync(
                seed.EmployeeId,
                new DateOnly(
                    2026,
                    9,
                    1))
            ?? throw new InvalidOperationException();

        EmployeeCompensation current =
            context.CurrentCompensation
            ?? throw new InvalidOperationException();

        current.Close(
            new DateOnly(
                2026,
                8,
                31));

        var replacement =
            new EmployeeCompensation(
                Guid.NewGuid(),
                seed.EmployeeId,
                seed.EmploymentPeriodId,
                new DateOnly(
                    2026,
                    9,
                    1),
                28_000_000m,
                "VND");

        await database.Persistence.ApplyAsync(
            current,
            replacement,
            "user-1",
            "admin");

        await using HrManagementDbContext verification =
            database.CreateContext();

        EmployeeCompensation[] rows =
            await verification
                .EmployeeCompensations
                .AsNoTracking()
                .OrderBy(
                    compensation =>
                        compensation.EffectiveFrom)
                .ToArrayAsync();

        Assert.Equal(
            2,
            rows.Length);

        Assert.Equal(
            new DateOnly(
                2026,
                8,
                31),
            rows[0].EffectiveTo);

        Assert.Equal(
            replacement.Id,
            rows[1].Id);

        Assert.True(
            rows[1].IsOpen);

        AuditEntry[] audits =
            await verification
                .AuditEntries
                .AsNoTracking()
                .OrderBy(
                    audit =>
                        audit.Action)
                .ThenBy(
                    audit =>
                        audit.EntityId)
                .ToArrayAsync();

        Assert.Equal(
            2,
            audits.Length);

        Assert.Contains(
            audits,
            audit =>
                audit.Action ==
                    AuditAction.Updated
                && audit.EntityId ==
                    current.Id);

        Assert.Contains(
            audits,
            audit =>
                audit.Action ==
                    AuditAction.Created
                && audit.EntityId ==
                    replacement.Id);

        Assert.All(
            audits,
            audit =>
            {
                Assert.Equal(
                    AuditEntityTypes.EmployeeCompensation,
                    audit.EntityType);

                Assert.Equal(
                    seed.EmployeeId,
                    audit.EmployeeId);

                Assert.Equal(
                    "user-1",
                    audit.ActorUserId);

                Assert.Equal(
                    "admin",
                    audit.ActorUsername);
            });
    }

    [Fact]
    public async Task ApplyAsync_WhenCurrentCompensationChanged_RejectsStaleWriteWithoutAudit()
    {
        await using TestDatabase database =
            await TestDatabase.CreateAsync();

        SeedResult seed =
            await database.SeedAsync(
                withOpenCompensation:
                    true);

        EmployeeCompensationContext staleContext =
            await database.ContextSource.GetAsync(
                seed.EmployeeId,
                new DateOnly(
                    2026,
                    9,
                    1))
            ?? throw new InvalidOperationException();

        EmployeeCompensation staleCurrent =
            staleContext.CurrentCompensation
            ?? throw new InvalidOperationException();

        staleCurrent.Close(
            new DateOnly(
                2026,
                8,
                31));

        var staleReplacement =
            new EmployeeCompensation(
                Guid.NewGuid(),
                seed.EmployeeId,
                seed.EmploymentPeriodId,
                new DateOnly(
                    2026,
                    9,
                    1),
                28_000_000m,
                "VND");

        await using (
            HrManagementDbContext competingContext =
                database.CreateContext())
        {
            EmployeeCompensation competingCurrent =
                await competingContext
                    .EmployeeCompensations
                    .SingleAsync();

            competingCurrent.Close(
                new DateOnly(
                    2026,
                    8,
                    15));

            competingContext
                .EmployeeCompensations
                .Add(
                    new EmployeeCompensation(
                        Guid.NewGuid(),
                        seed.EmployeeId,
                        seed.EmploymentPeriodId,
                        new DateOnly(
                            2026,
                            8,
                            16),
                        27_000_000m,
                        "VND"));

            await competingContext
                .SaveChangesAsync();
        }

        await Assert.ThrowsAsync<
            EmployeeCompensationConcurrencyException>(
            () =>
                database.Persistence.ApplyAsync(
                    staleCurrent,
                    staleReplacement,
                    "user-1",
                    "admin"));

        await using HrManagementDbContext verification =
            database.CreateContext();

        Assert.Equal(
            2,
            await verification
                .EmployeeCompensations
                .CountAsync());

        Assert.Empty(
            await verification
                .AuditEntries
                .AsNoTracking()
                .ToArrayAsync());
    }

    [Fact]
    public async Task ApplyAsync_WhenHistoricalRangeOverlaps_RejectsWithoutWrites()
    {
        await using TestDatabase database =
            await TestDatabase.CreateAsync();

        SeedResult seed =
            await database.SeedAsync(
                withOpenCompensation:
                    false);

        await using (
            HrManagementDbContext setupContext =
                database.CreateContext())
        {
            setupContext
                .EmployeeCompensations
                .Add(
                    new EmployeeCompensation(
                        Guid.NewGuid(),
                        seed.EmployeeId,
                        seed.EmploymentPeriodId,
                        new DateOnly(
                            2026,
                            9,
                            1),
                        25_000_000m,
                        "VND",
                        new DateOnly(
                            2026,
                            9,
                            30)));

            await setupContext
                .SaveChangesAsync();
        }

        var overlapping =
            new EmployeeCompensation(
                Guid.NewGuid(),
                seed.EmployeeId,
                seed.EmploymentPeriodId,
                new DateOnly(
                    2026,
                    9,
                    15),
                28_000_000m,
                "VND");

        await Assert.ThrowsAsync<
            EmployeeCompensationConcurrencyException>(
            () =>
                database.Persistence.ApplyAsync(
                    null,
                    overlapping,
                    "user-1",
                    "admin"));

        await using HrManagementDbContext verification =
            database.CreateContext();

        Assert.Equal(
            1,
            await verification
                .EmployeeCompensations
                .CountAsync());

        Assert.Empty(
            await verification
                .AuditEntries
                .ToArrayAsync());
    }

    [Fact]
    public async Task ApplyAsync_WhenAuditActorDoesNotMatch_RejectsWithoutWrites()
    {
        await using TestDatabase database =
            await TestDatabase.CreateAsync();

        SeedResult seed =
            await database.SeedAsync(
                withOpenCompensation:
                    false);

        var compensation =
            new EmployeeCompensation(
                Guid.NewGuid(),
                seed.EmployeeId,
                seed.EmploymentPeriodId,
                new DateOnly(
                    2026,
                    8,
                    1),
                25_000_000m,
                "VND");

        await Assert.ThrowsAsync<InvalidOperationException>(
            () =>
                database.Persistence.ApplyAsync(
                    null,
                    compensation,
                    "different-user",
                    "different-user"));

        await using HrManagementDbContext verification =
            database.CreateContext();

        Assert.Empty(
            await verification
                .EmployeeCompensations
                .ToArrayAsync());

        Assert.Empty(
            await verification
                .AuditEntries
                .ToArrayAsync());
    }

    private sealed record SeedResult(
        Guid EmployeeId,
        Guid EmploymentPeriodId,
        Guid? CompensationId);

    private sealed class TestDatabase
        : IAsyncDisposable
    {
        private readonly SqliteConnection
            _connection;

        private readonly DbContextOptions<HrManagementDbContext>
            _options;

        public EfEmployeeCompensationContextSource ContextSource
        {
            get;
        }

        public EfEmployeeCompensationPersistence Persistence
        {
            get;
        }

        private TestDatabase(
            SqliteConnection connection,
            DbContextOptions<HrManagementDbContext> options,
            EfEmployeeCompensationContextSource contextSource,
            EfEmployeeCompensationPersistence persistence)
        {
            _connection =
                connection;

            _options =
                options;

            ContextSource =
                contextSource;

            Persistence =
                persistence;
        }

        public static async Task<TestDatabase> CreateAsync()
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

            var currentUserContext =
                new StubCurrentUserContext(
                    new AuthenticatedUser(
                        "user-1",
                        "admin",
                        "Administrator"));

            var auditFactory =
                new AuditEntryFactory(
                    currentUserContext,
                    new FixedTimeProvider(
                        new DateTimeOffset(
                            2026,
                            9,
                            2,
                            2,
                            0,
                            0,
                            TimeSpan.Zero)));

            return new TestDatabase(
                connection,
                options,
                new EfEmployeeCompensationContextSource(
                    factory),
                new EfEmployeeCompensationPersistence(
                    factory,
                    auditFactory));
        }

        public HrManagementDbContext CreateContext()
        {
            return new HrManagementDbContext(
                _options);
        }

        public async Task<SeedResult> SeedAsync(
            bool withOpenCompensation)
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
                    email:
                        null,
                    phoneNumber:
                        null,
                    dateOfBirth:
                        null,
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

            EmployeeCompensation? compensation =
                withOpenCompensation
                    ? new EmployeeCompensation(
                        Guid.NewGuid(),
                        employeeId,
                        employmentPeriodId,
                        new DateOnly(
                            2026,
                            8,
                            1),
                        25_000_000m,
                        "VND")
                    : null;

            await using HrManagementDbContext dbContext =
                CreateContext();

            dbContext.Employees.Add(
                employee);

            dbContext.EmploymentPeriods.Add(
                employmentPeriod);

            if (compensation is not null)
            {
                dbContext.EmployeeCompensations.Add(
                    compensation);
            }

            await dbContext.SaveChangesAsync();

            return new SeedResult(
                employeeId,
                employmentPeriodId,
                compensation?.Id);
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

    private sealed class StubCurrentUserContext
        : ICurrentUserContext
    {
        public StubCurrentUserContext(
            AuthenticatedUser currentUser)
        {
            CurrentUser =
                currentUser;
        }

        public AuthenticatedUser? CurrentUser
        {
            get;
        }

        public bool IsAuthenticated =>
            CurrentUser is not null;
    }

    private sealed class FixedTimeProvider
        : TimeProvider
    {
        private readonly DateTimeOffset
            _utcNow;

        public FixedTimeProvider(
            DateTimeOffset utcNow)
        {
            _utcNow =
                utcNow;
        }

        public override DateTimeOffset GetUtcNow()
        {
            return _utcNow;
        }

        public override TimeZoneInfo LocalTimeZone =>
            TimeZoneInfo.Utc;
    }
}
