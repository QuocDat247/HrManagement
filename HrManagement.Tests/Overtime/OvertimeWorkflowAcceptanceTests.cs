using HrManagement.Infrastructure.Payroll.Periods;
using HrManagement.Application.Auditing;
using HrManagement.Application.Authentication;
using HrManagement.Application.Overtime.Requests;
using HrManagement.Application.Workspaces.Overtime;
using HrManagement.Domain.Auditing;
using HrManagement.Domain.Employees;
using HrManagement.Domain.Overtime.Requests;
using HrManagement.Infrastructure.Attendance.Timesheets;
using HrManagement.Infrastructure.Employees;
using HrManagement.Infrastructure.Overtime.Requests;
using HrManagement.Infrastructure.Persistence;
using HrManagement.Infrastructure.Workspaces.Overtime;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace HrManagement.Tests.Overtime;

public sealed class OvertimeWorkflowAcceptanceTests
{
    [Fact]
    public async Task Workflow_SubmitApproveCancel_RemainsConsistentAcrossQueriesHistoryAndAudit()
    {
        await using TestDatabase database =
            await TestDatabase.CreateAsync();

        SeedResult seed =
            await database.SeedEmployeeAsync();

        database.TimeProvider.SetUtcNow(
            Utc(
                10));

        SubmitOvertimeRequestResult submitResult =
            await database.SubmitService.SubmitAsync(
                new SubmitOvertimeRequestRequest(
                    seed.EmployeeId,
                    seed.WorkDate,
                    120,
                    "Hỗ trợ triển khai"));

        Assert.True(
            submitResult.IsSuccessful);

        Assert.NotNull(
            submitResult.OvertimeRequestId);

        Assert.Equal(
            OvertimeRequestStatus.Pending,
            submitResult.Status);

        Guid requestId =
            submitResult.OvertimeRequestId!.Value;

        OvertimeWorkspaceSnapshot pendingSnapshot =
            await database.QueryService.GetAsync(
                new OvertimeWorkspaceQuery(
                    seed.WorkDate.Year,
                    seed.WorkDate.Month,
                    seed.EmployeeId));

        OvertimeWorkspaceItem pendingItem =
            Assert.Single(
                pendingSnapshot.Requests);

        Assert.Equal(
            requestId,
            pendingItem.OvertimeRequestId);

        Assert.Equal(
            OvertimeRequestStatus.Pending,
            pendingItem.Status);

        Assert.Null(
            pendingItem.ApprovedMinutes);

        database.TimeProvider.SetUtcNow(
            Utc(
                11));

        ChangeOvertimeRequestStatusResult approveResult =
            await database.StatusService.ChangeStatusAsync(
                new ChangeOvertimeRequestStatusRequest(
                    requestId,
                    OvertimeRequestStatus.Pending,
                    OvertimeRequestStatus.Approved,
                    ApprovedMinutes:
                        90,
                    Note:
                        "Duyệt một phần"));

        Assert.True(
            approveResult.IsSuccessful);

        Assert.Equal(
            OvertimeRequestStatus.Approved,
            approveResult.Status);

        Assert.Equal(
            90,
            approveResult.ApprovedMinutes);

        OvertimeWorkspaceSnapshot approvedSnapshot =
            await database.QueryService.GetAsync(
                new OvertimeWorkspaceQuery(
                    seed.WorkDate.Year,
                    seed.WorkDate.Month,
                    seed.EmployeeId));

        OvertimeWorkspaceItem approvedItem =
            Assert.Single(
                approvedSnapshot.Requests);

        Assert.Equal(
            OvertimeRequestStatus.Approved,
            approvedItem.Status);

        Assert.Equal(
            90,
            approvedItem.ApprovedMinutes);

        IReadOnlyList<OvertimeStatusHistoryItem>
            approvedHistory =
                await database.QueryService
                    .GetHistoryAsync(
                        requestId);

        OvertimeStatusHistoryItem approval =
            Assert.Single(
                approvedHistory);

        Assert.Equal(
            OvertimeRequestStatus.Pending,
            approval.PreviousStatus);

        Assert.Equal(
            OvertimeRequestStatus.Approved,
            approval.NewStatus);

        Assert.Equal(
            90,
            approval.ApprovedMinutes);

        Assert.Equal(
            "Duyệt một phần",
            approval.Note);

        database.TimeProvider.SetUtcNow(
            Utc(
                12));

        ChangeOvertimeRequestStatusResult cancelResult =
            await database.StatusService.ChangeStatusAsync(
                new ChangeOvertimeRequestStatusRequest(
                    requestId,
                    OvertimeRequestStatus.Approved,
                    OvertimeRequestStatus.Cancelled,
                    Note:
                        "Không còn nhu cầu tăng ca"));

        Assert.True(
            cancelResult.IsSuccessful);

        Assert.Equal(
            OvertimeRequestStatus.Cancelled,
            cancelResult.Status);

        Assert.Null(
            cancelResult.ApprovedMinutes);

        OvertimeWorkspaceSnapshot cancelledSnapshot =
            await database.QueryService.GetAsync(
                new OvertimeWorkspaceQuery(
                    seed.WorkDate.Year,
                    seed.WorkDate.Month,
                    seed.EmployeeId));

        OvertimeWorkspaceItem cancelledItem =
            Assert.Single(
                cancelledSnapshot.Requests);

        Assert.Equal(
            OvertimeRequestStatus.Cancelled,
            cancelledItem.Status);

        Assert.Null(
            cancelledItem.ApprovedMinutes);

        IReadOnlyList<OvertimeStatusHistoryItem>
            finalHistory =
                await database.QueryService
                    .GetHistoryAsync(
                        requestId);

        Assert.Equal(
            2,
            finalHistory.Count);

        Assert.Equal(
            OvertimeRequestStatus.Approved,
            finalHistory[0].PreviousStatus);

        Assert.Equal(
            OvertimeRequestStatus.Cancelled,
            finalHistory[0].NewStatus);

        Assert.Equal(
            "Không còn nhu cầu tăng ca",
            finalHistory[0].Note);

        Assert.Equal(
            OvertimeRequestStatus.Pending,
            finalHistory[1].PreviousStatus);

        Assert.Equal(
            OvertimeRequestStatus.Approved,
            finalHistory[1].NewStatus);

        await using HrManagementDbContext verification =
            await database.Factory
                .CreateDbContextAsync();

        AuditEntry[] audits =
            await verification
                .AuditEntries
                .AsNoTracking()
                .OrderBy(
                    audit =>
                        audit.OccurredAtUtc)
                .ToArrayAsync();

        Assert.Equal(
            3,
            audits.Length);

        Assert.Equal(
            AuditAction.Created,
            audits[0].Action);

        Assert.Equal(
            AuditAction.Updated,
            audits[1].Action);

        Assert.Equal(
            AuditAction.Updated,
            audits[2].Action);

        Assert.All(
            audits,
            audit =>
            {
                Assert.Equal(
                    AuditEntityTypes.OvertimeRequest,
                    audit.EntityType);

                Assert.Equal(
                    requestId,
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
            });
    }

    private sealed record SeedResult(
        Guid EmployeeId,
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

        public MutableTimeProvider TimeProvider
        {
            get;
        }

        public SubmitOvertimeRequestService SubmitService
        {
            get;
        }

        public OvertimeRequestStatusService StatusService
        {
            get;
        }

        public EfOvertimeWorkspaceQueryService QueryService
        {
            get;
        }

        private TestDatabase(
            SqliteConnection connection,
            TestDbContextFactory factory,
            MutableTimeProvider timeProvider,
            SubmitOvertimeRequestService submitService,
            OvertimeRequestStatusService statusService,
            EfOvertimeWorkspaceQueryService queryService)
        {
            _connection =
                connection;

            Factory =
                factory;

            TimeProvider =
                timeProvider;

            SubmitService =
                submitService;

            StatusService =
                statusService;

            QueryService =
                queryService;
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

            var timeProvider =
                new MutableTimeProvider(
                    Utc(
                        9));

            var auditFactory =
                new AuditEntryFactory(
                    currentUserContext,
                    timeProvider);

            var employeeRepository =
                new EfEmployeeRepository(
                    factory);

            var submissionContextSource =
                new EfOvertimeRequestSubmissionContextSource(
                    factory);

            var periodLockPolicy =
                new EfAttendancePeriodLockPolicy(
                    factory);

            var submissionPersistence =
                new EfOvertimeRequestSubmissionPersistence(
                    factory,
                    auditFactory);

            var submitService =
                new SubmitOvertimeRequestService(
                    employeeRepository,
                    submissionContextSource,
                    periodLockPolicy,
                    new AuthenticatedOvertimeRequestSubmissionAuthorizationPolicy(),
                    submissionPersistence,
                    currentUserContext,
                    timeProvider);

            var statusContextSource =
                new EfOvertimeRequestStatusTransitionContextSource(
                    factory);

            var statusPersistence =
                new EfOvertimeRequestStatusTransitionPersistence(
                    factory,
                    auditFactory);

            var financialPeriodLockSource =
                new EfPayrollFinancialPeriodLockSource(
                    factory);

            var statusService =
                new OvertimeRequestStatusService(
                    statusContextSource,
                    statusPersistence,
                    new AuthenticatedOvertimeRequestStatusAuthorizationPolicy(),
                    financialPeriodLockSource,
                    currentUserContext,
                    timeProvider);

            var queryService =
                new EfOvertimeWorkspaceQueryService(
                    factory);

            return new TestDatabase(
                connection,
                factory,
                timeProvider,
                submitService,
                statusService,
                queryService);
        }

        public async Task<SeedResult> SeedEmployeeAsync()
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
                await Factory
                    .CreateDbContextAsync();

            dbContext.Employees.Add(
                employee);

            dbContext.EmploymentPeriods.Add(
                employmentPeriod);

            await dbContext.SaveChangesAsync();

            return new SeedResult(
                employeeId,
                new DateOnly(
                    2026,
                    8,
                    27));
        }

        public async ValueTask DisposeAsync()
        {
            await _connection.DisposeAsync();
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

    private sealed class MutableTimeProvider
        : TimeProvider
    {
        private DateTimeOffset
            _utcNow;

        public MutableTimeProvider(
            DateTime utcNow)
        {
            _utcNow =
                new DateTimeOffset(
                    utcNow);
        }

        public void SetUtcNow(
            DateTime utcNow)
        {
            _utcNow =
                new DateTimeOffset(
                    utcNow);
        }

        public override DateTimeOffset GetUtcNow()
        {
            return _utcNow;
        }

        public override TimeZoneInfo LocalTimeZone =>
            TimeZoneInfo.Utc;
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
