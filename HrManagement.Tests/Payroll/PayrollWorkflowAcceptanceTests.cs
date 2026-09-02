using HrManagement.Application.Attendance.Timesheets;
using HrManagement.Application.Auditing;
using HrManagement.Application.Authentication;
using HrManagement.Application.Payroll.Calculations;
using HrManagement.Application.Payroll.Periods;
using HrManagement.Domain.Attendance.Calculations;
using HrManagement.Domain.Attendance.Records;
using HrManagement.Domain.Attendance.Schedules;
using HrManagement.Domain.Attendance.Timesheets;
using HrManagement.Domain.Auditing;
using HrManagement.Domain.Employees;
using HrManagement.Domain.Overtime.Requests;
using HrManagement.Domain.Payroll.Compensation;
using HrManagement.Infrastructure.Attendance.Timesheets;
using HrManagement.Infrastructure.Payroll.Calculations;
using HrManagement.Infrastructure.Payroll.Compensation;
using HrManagement.Infrastructure.Payroll.Periods;
using HrManagement.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace HrManagement.Tests.Payroll;

public sealed class PayrollWorkflowAcceptanceTests
{
    [Fact]
    public async Task Workflow_ClosedTimesheetPreviewCloseAndReadSnapshot_RemainsImmutableAndLocked()
    {
        await using TestDatabase database =
            await TestDatabase.CreateAsync();

        SeedResult seed =
            await database.SeedAsync();

        PayrollPreview preview =
            await database.PreviewService
                .GetAsync(
                    2026,
                    8);

        Assert.True(
            preview.IsFinalizable);

        Assert.Equal(
            seed.TimesheetPeriodId,
            preview.TimesheetPeriodId);

        PayrollEmployeePreview previewEmployee =
            Assert.Single(
                preview.Employees);

        Assert.Equal(
            seed.EmployeeId,
            previewEmployee.EmployeeId);

        Assert.Equal(
            "EMP001",
            previewEmployee.EmployeeCode);

        Assert.Equal(
            "Nguyễn Văn An",
            previewEmployee.EmployeeFullName);

        Assert.Equal(
            "VND",
            previewEmployee.CurrencyCode);

        Assert.Equal(
            12_480_000m,
            previewEmployee.BaseSalaryAmount);

        Assert.Equal(
            120,
            previewEmployee.ApprovedOvertimeMinutes);

        Assert.Equal(
            120,
            previewEmployee.PayableOvertimeMinutes);

        Assert.Equal(
            240_000m,
            previewEmployee.OvertimeAmount);

        Assert.Equal(
            12_720_000m,
            previewEmployee.GrossAmount);

        ClosePayrollPeriodResult closeResult =
            await database.CloseService
                .CloseAsync(
                    new ClosePayrollPeriodRequest(
                        2026,
                        8));

        Assert.True(
            closeResult.IsSuccessful);

        Assert.NotNull(
            closeResult.PayrollPeriodId);

        Assert.Equal(
            1,
            closeResult.SnapshotCount);

        ClosedPayrollReadModel closed =
            await database.ClosedQueryService
                .GetAsync(
                    2026,
                    8)
            ?? throw new InvalidOperationException(
                "Không đọc được kỳ lương vừa đóng.");

        Assert.Equal(
            closeResult.PayrollPeriodId,
            closed.PayrollPeriodId);

        Assert.Equal(
            seed.TimesheetPeriodId,
            closed.TimesheetPeriodId);

        Assert.Equal(
            "user-1",
            closed.ClosedByUserId);

        Assert.Equal(
            "admin",
            closed.ClosedByUsername);

        Assert.Equal(
            1,
            closed.SnapshotCount);

        ClosedPayrollEmployeeItem closedEmployee =
            Assert.Single(
                closed.Employees);

        Assert.Equal(
            "EMP001",
            closedEmployee.EmployeeCode);

        Assert.Equal(
            "Nguyễn Văn An",
            closedEmployee.EmployeeFullName);

        Assert.Equal(
            12_480_000m,
            closedEmployee.BaseSalaryAmount);

        Assert.Equal(
            120,
            closedEmployee.ApprovedOvertimeMinutes);

        Assert.Equal(
            120,
            closedEmployee.PayableOvertimeMinutes);

        Assert.Equal(
            240_000m,
            closedEmployee.OvertimeAmount);

        Assert.Equal(
            12_720_000m,
            closedEmployee.GrossAmount);

        ClosedPayrollCurrencySummary summary =
            Assert.Single(
                closed.CurrencySummaries);

        Assert.Equal(
            "VND",
            summary.CurrencyCode);

        Assert.Equal(
            1,
            summary.EmployeeCount);

        Assert.Equal(
            12_480_000m,
            summary.BaseSalaryAmount);

        Assert.Equal(
            240_000m,
            summary.OvertimeAmount);

        Assert.Equal(
            12_720_000m,
            summary.GrossAmount);

        bool payrollLocked =
            await database.FinancialLockSource
                .IsLockedAsync(
                    new DateOnly(
                        2026,
                        8,
                        1),
                    new DateOnly(
                        2026,
                        8,
                        31));

        Assert.True(
            payrollLocked);

        await database.ChangeLiveEmployeeIdentityAsync(
            seed.EmployeeId);

        ClosedPayrollReadModel afterLiveChange =
            await database.ClosedQueryService
                .GetAsync(
                    2026,
                    8)
            ?? throw new InvalidOperationException();

        ClosedPayrollEmployeeItem immutableEmployee =
            Assert.Single(
                afterLiveChange.Employees);

        Assert.Equal(
            "EMP001",
            immutableEmployee.EmployeeCode);

        Assert.Equal(
            "Nguyễn Văn An",
            immutableEmployee.EmployeeFullName);

        Assert.Equal(
            12_720_000m,
            immutableEmployee.GrossAmount);

        ClosePayrollPeriodResult duplicateClose =
            await database.CloseService
                .CloseAsync(
                    new ClosePayrollPeriodRequest(
                        2026,
                        8));

        Assert.False(
            duplicateClose.IsSuccessful);

        await using HrManagementDbContext verification =
            database.CreateContext();

        Assert.Equal(
            1,
            await verification
                .PayrollPeriods
                .CountAsync());

        Assert.Equal(
            1,
            await verification
                .PayrollEmployeeSnapshots
                .CountAsync());

        AuditEntry[] payrollAudits =
            await verification
                .AuditEntries
                .AsNoTracking()
                .Where(
                    audit =>
                        audit.EntityType ==
                            AuditEntityTypes.PayrollPeriod
                        || audit.EntityType ==
                            AuditEntityTypes.PayrollEmployeeSnapshot)
                .ToArrayAsync();

        Assert.Equal(
            2,
            payrollAudits.Length);

        Assert.All(
            payrollAudits,
            audit =>
            {
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
        Guid TimesheetPeriodId);

    private sealed class TestDatabase
        : IAsyncDisposable
    {
        private readonly SqliteConnection
            _connection;

        private readonly DbContextOptions<HrManagementDbContext>
            _options;

        private TestDatabase(
            SqliteConnection connection,
            DbContextOptions<HrManagementDbContext> options,
            TestDbContextFactory factory,
            PayrollPreviewService previewService,
            ClosePayrollPeriodService closeService,
            EfClosedPayrollQueryService closedQueryService,
            EfPayrollFinancialPeriodLockSource financialLockSource)
        {
            _connection =
                connection;

            _options =
                options;

            Factory =
                factory;

            PreviewService =
                previewService;

            CloseService =
                closeService;

            ClosedQueryService =
                closedQueryService;

            FinancialLockSource =
                financialLockSource;
        }

        public TestDbContextFactory Factory
        {
            get;
        }

        public PayrollPreviewService PreviewService
        {
            get;
        }

        public ClosePayrollPeriodService CloseService
        {
            get;
        }

        public EfClosedPayrollQueryService ClosedQueryService
        {
            get;
        }

        public EfPayrollFinancialPeriodLockSource FinancialLockSource
        {
            get;
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
                    await factory
                        .CreateDbContextAsync())
            {
                await dbContext.Database
                    .EnsureCreatedAsync();
            }

            var timesheetQueryService =
                new MonthlyTimesheetQueryService(
                    new EfMonthlyTimesheetQuerySource(
                        factory));

            var compensationQuerySource =
                new EfEmployeeCompensationQuerySource(
                    factory);

            var overtimeSource =
                new EfApprovedOvertimePayrollSource(
                    factory);

            var inputService =
                new PayrollCalculationInputService(
                    timesheetQueryService,
                    compensationQuerySource,
                    overtimeSource);

            var baseSalaryPolicy =
                new CalendarDayBaseSalaryProrationPolicy();

            var overtimeResolver =
                new EmployeeOvertimePayabilityResolver(
                    new ConservativeOvertimePayabilityPolicy());

            var payrollPolicyProfile =
                new PayrollPolicyProfile(
                    standardMonthlyWorkingMinutes:
                        12_480,
                    nonWorkingDayOvertimeMultiplier:
                        2m);

            var overtimeAmountPolicy =
                new ConfiguredOvertimeAmountPolicy(
                    new ConfiguredPayrollPolicyProfileSource(
                        payrollPolicyProfile));

            var moneyRoundingPolicy =
                new CurrencyMoneyRoundingPolicy();

            var previewService =
                new PayrollPreviewService(
                    inputService,
                    baseSalaryPolicy,
                    overtimeResolver,
                    overtimeAmountPolicy,
                    moneyRoundingPolicy);

            var closePersistence =
                new EfClosePayrollPeriodPersistence(
                    factory,
                    baseSalaryPolicy,
                    overtimeResolver,
                    overtimeAmountPolicy,
                    moneyRoundingPolicy);

            var currentUserContext =
                new StubCurrentUserContext(
                    new AuthenticatedUser(
                        "user-1",
                        "admin",
                        "Administrator"));

            var closeService =
                new ClosePayrollPeriodService(
                    previewService,
                    closePersistence,
                    new AuthenticatedPayrollPeriodClosingAuthorizationPolicy(),
                    currentUserContext,
                    new FixedTimeProvider(
                        new DateTimeOffset(
                            Utc(
                                20))));

            return new TestDatabase(
                connection,
                options,
                factory,
                previewService,
                closeService,
                new EfClosedPayrollQueryService(
                    factory),
                new EfPayrollFinancialPeriodLockSource(
                    factory));
        }

        public HrManagementDbContext CreateContext()
        {
            return new HrManagementDbContext(
                _options);
        }

        public async Task<SeedResult> SeedAsync()
        {
            Guid employeeId =
                Guid.NewGuid();

            Guid employmentPeriodId =
                Guid.NewGuid();

            Guid scheduleId =
                Guid.NewGuid();

            Guid assignmentId =
                Guid.NewGuid();

            Guid timesheetPeriodId =
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

            var schedule =
                new WorkSchedule(
                    scheduleId,
                    "TEST",
                    "Test schedule",
                    "UTC");

            var assignment =
                new EmployeeWorkScheduleAssignment(
                    assignmentId,
                    employeeId,
                    employmentPeriodId,
                    scheduleId,
                    new DateOnly(
                        2026,
                        1,
                        1));

            var compensation =
                new EmployeeCompensation(
                    Guid.NewGuid(),
                    employeeId,
                    employmentPeriodId,
                    new DateOnly(
                        2026,
                        8,
                        1),
                    12_480_000m,
                    "VND");

            var timesheetPeriod =
                new TimesheetPeriod(
                    timesheetPeriodId,
                    2026,
                    8);

            timesheetPeriod.Close(
                Utc(
                    18),
                "timesheet-user",
                "timesheet-admin");

            var attendanceRecords =
                new List<AttendanceRecord>();

            var timesheetSnapshots =
                new List<MonthlyTimesheetDaySnapshot>();

            DateOnly overtimeWorkDate =
                new(
                    2026,
                    8,
                    10);

            for (
                int dayNumber = 1;
                dayNumber <= 31;
                dayNumber++)
            {
                DateOnly workDate =
                    new(
                        2026,
                        8,
                        dayNumber);

                Guid attendanceRecordId =
                    Guid.NewGuid();

                var attendanceRecord =
                    new AttendanceRecord(
                        attendanceRecordId,
                        employeeId,
                        employmentPeriodId,
                        assignmentId,
                        scheduleId,
                        workDate,
                        "UTC",
                        isWorkingDay:
                            false);

                int workedMinutes =
                    workDate ==
                        overtimeWorkDate
                        ? 120
                        : 0;

                var timesheetSnapshot =
                    new MonthlyTimesheetDaySnapshot(
                        Guid.NewGuid(),
                        timesheetPeriodId,
                        attendanceRecordId,
                        employeeId,
                        workDate,
                        isWorkingDay:
                            false,
                        expectedPlannedMinutes:
                            0,
                        AttendanceCalculationStatus
                            .NonWorkingDay,
                        workedMinutes,
                        lateMinutes:
                            0,
                        earlyLeaveMinutes:
                            0,
                        correctionRevision:
                            0);

                attendanceRecords.Add(
                    attendanceRecord);

                timesheetSnapshots.Add(
                    timesheetSnapshot);
            }

            var overtimeRequest =
                new OvertimeRequest(
                    Guid.NewGuid(),
                    employeeId,
                    employmentPeriodId,
                    overtimeWorkDate,
                    120,
                    "Payroll acceptance",
                    Utc(
                        7));

            overtimeRequest.TransitionTo(
                Guid.NewGuid(),
                OvertimeRequestStatus.Approved,
                Utc(
                    8),
                "ot-user",
                "ot-admin",
                approvedMinutes:
                    120);

            await using HrManagementDbContext dbContext =
                CreateContext();

            dbContext.Employees.Add(
                employee);

            dbContext.EmploymentPeriods.Add(
                employmentPeriod);

            dbContext.WorkSchedules.Add(
                schedule);

            dbContext.EmployeeWorkScheduleAssignments.Add(
                assignment);

            dbContext.EmployeeCompensations.Add(
                compensation);

            dbContext.TimesheetPeriods.Add(
                timesheetPeriod);

            dbContext.AttendanceRecords.AddRange(
                attendanceRecords);

            dbContext.MonthlyTimesheetDaySnapshots.AddRange(
                timesheetSnapshots);

            dbContext.OvertimeRequests.Add(
                overtimeRequest);

            await dbContext.SaveChangesAsync();

            return new SeedResult(
                employeeId,
                timesheetPeriodId);
        }

        public async Task ChangeLiveEmployeeIdentityAsync(
            Guid employeeId)
        {
            await using HrManagementDbContext dbContext =
                CreateContext();

            await dbContext
                .Employees
                .Where(
                    employee =>
                        employee.Id ==
                        employeeId)
                .ExecuteUpdateAsync(
                    setters =>
                        setters
                            .SetProperty(
                                employee =>
                                    employee.EmployeeCode,
                                "EMP999")
                            .SetProperty(
                                employee =>
                                    employee.FullName,
                                "Tên đã thay đổi"));
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

    private static DateTime Utc(
        int hour)
    {
        return new DateTime(
            2026,
            8,
            31,
            hour,
            0,
            0,
            DateTimeKind.Utc);
    }
}
