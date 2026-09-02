using HrManagement.Application.Auditing;
using HrManagement.Application.Authentication;
using HrManagement.Application.Employees;
using HrManagement.Application.Payroll.Compensation;
using HrManagement.Domain.Auditing;
using HrManagement.Domain.Employees;
using HrManagement.Infrastructure.Employees;
using HrManagement.Infrastructure.Payroll.Compensation;
using HrManagement.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace HrManagement.Tests.Payroll;

public sealed class EmployeeCompensationWorkflowAcceptanceTests
{
    [Fact]
    public async Task Workflow_InitialSalaryThenRevision_ProducesContiguousHistoryAndMetadataAudit()
    {
        await using TestDatabase database =
            await TestDatabase.CreateAsync();

        Guid employeeId =
            await database.SeedEmployeeAsync();

        SetEmployeeCompensationResult initial =
            await database.Service.SetAsync(
                new SetEmployeeCompensationRequest(
                    employeeId,
                    new DateOnly(
                        2026,
                        8,
                        1),
                    25_000_000m,
                    "vnd"));

        Assert.True(
            initial.IsSuccessful);

        Assert.NotNull(
            initial.CompensationId);

        Assert.Null(
            initial.PreviousCompensationId);

        SetEmployeeCompensationResult revision =
            await database.Service.SetAsync(
                new SetEmployeeCompensationRequest(
                    employeeId,
                    new DateOnly(
                        2026,
                        8,
                        16),
                    28_000_000m,
                    "VND"));

        Assert.True(
            revision.IsSuccessful);

        Assert.NotNull(
            revision.CompensationId);

        Assert.Equal(
            initial.CompensationId,
            revision.PreviousCompensationId);

        IReadOnlyList<EmployeeCompensationSegment> august =
            await database.QuerySource
                .GetForPeriodAsync(
                    [employeeId],
                    new DateOnly(
                        2026,
                        8,
                        1),
                    new DateOnly(
                        2026,
                        8,
                        31));

        Assert.Equal(
            2,
            august.Count);

        EmployeeCompensationSegment first =
            august[0];

        EmployeeCompensationSegment second =
            august[1];

        Assert.Equal(
            new DateOnly(
                2026,
                8,
                1),
            first.EffectiveFrom);

        Assert.Equal(
            new DateOnly(
                2026,
                8,
                15),
            first.EffectiveTo);

        Assert.Equal(
            25_000_000m,
            first.MonthlyBaseSalary);

        Assert.Equal(
            new DateOnly(
                2026,
                8,
                16),
            second.EffectiveFrom);

        Assert.Null(
            second.EffectiveTo);

        Assert.Equal(
            28_000_000m,
            second.MonthlyBaseSalary);

        Assert.Equal(
            first.EffectiveTo!.Value.AddDays(
                1),
            second.EffectiveFrom);

        await using HrManagementDbContext verification =
            database.CreateContext();

        AuditEntry[] audits =
            await verification
                .AuditEntries
                .AsNoTracking()
                .OrderBy(
                    audit =>
                        audit.Id)
                .ToArrayAsync();

        Assert.Equal(
            3,
            audits.Length);

        Assert.Equal(
            2,
            audits.Count(
                audit =>
                    audit.Action ==
                        AuditAction.Created));

        Assert.Equal(
            1,
            audits.Count(
                audit =>
                    audit.Action ==
                        AuditAction.Updated));

        Assert.All(
            audits,
            audit =>
            {
                Assert.Equal(
                    AuditEntityTypes.EmployeeCompensation,
                    audit.EntityType);

                Assert.Equal(
                    employeeId,
                    audit.EmployeeId);

                Assert.Equal(
                    "user-1",
                    audit.ActorUserId);

                Assert.Equal(
                    "admin",
                    audit.ActorUsername);
            });
    }

    private sealed class TestDatabase
        : IAsyncDisposable
    {
        private readonly SqliteConnection
            _connection;

        private readonly DbContextOptions<HrManagementDbContext>
            _options;

        public EmployeeCompensationService Service
        {
            get;
        }

        public EfEmployeeCompensationQuerySource QuerySource
        {
            get;
        }

        private TestDatabase(
            SqliteConnection connection,
            DbContextOptions<HrManagementDbContext> options,
            EmployeeCompensationService service,
            EfEmployeeCompensationQuerySource querySource)
        {
            _connection =
                connection;

            _options =
                options;

            Service =
                service;

            QuerySource =
                querySource;
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

            var contextSource =
                new EfEmployeeCompensationContextSource(
                    factory);

            var persistence =
                new EfEmployeeCompensationPersistence(
                    factory,
                    auditFactory);

            var service =
                new EmployeeCompensationService(
                    new EfEmployeeRepository(
                        factory),
                    contextSource,
                    persistence,
                    new AuthenticatedEmployeeCompensationAuthorizationPolicy(),
                    currentUserContext);

            return new TestDatabase(
                connection,
                options,
                service,
                new EfEmployeeCompensationQuerySource(
                    factory));
        }

        public async Task<Guid> SeedEmployeeAsync()
        {
            Guid employeeId =
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
                    Guid.NewGuid(),
                    employeeId,
                    new DateOnly(
                        2026,
                        1,
                        1));

            await using HrManagementDbContext dbContext =
                CreateContext();

            dbContext.Employees.Add(
                employee);

            dbContext.EmploymentPeriods.Add(
                employmentPeriod);

            await dbContext.SaveChangesAsync();

            return employeeId;
        }

        public HrManagementDbContext CreateContext()
        {
            return new HrManagementDbContext(
                _options);
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
