using HrManagement.Application.Auditing;
using HrManagement.Application.Payroll.Calculations;
using HrManagement.Domain.Attendance.Calculations;
using HrManagement.Domain.Attendance.Records;
using HrManagement.Domain.Attendance.Schedules;
using HrManagement.Domain.Attendance.Timesheets;
using HrManagement.Domain.Auditing;
using HrManagement.Domain.Employees;
using HrManagement.Domain.Overtime.Requests;
using HrManagement.Domain.Payroll.Compensation;
using HrManagement.Domain.Payroll.Periods;
using HrManagement.Domain.Payroll.Snapshots;
using HrManagement.Infrastructure.Payroll.Calculations;
using HrManagement.Infrastructure.Payroll.Periods;
using HrManagement.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace HrManagement.Tests.Payroll;

public sealed class EfClosePayrollPeriodPersistenceTests
{
    [Fact]
    public async Task PersistAsync_WhenSourceStillMatches_WritesPeriodSnapshotsAndAuditsAtomically()
    {
        await using TestDatabase database =
            await TestDatabase.CreateAsync();

        SeedResult seed =
            await database.SeedAsync();

        var persistence =
            CreatePersistence(
                database);

        PayrollPeriod period =
            CreateClosedPayrollPeriod(
                seed.TimesheetPeriodId);

        PayrollEmployeeSnapshot snapshot =
            CreateSnapshot(
                period.Id,
                seed.EmployeeId,
                baseSalary:
                    12_480_000m);

        await persistence.PersistAsync(
            period,
            [snapshot],
            "user-1",
            "admin");

        await using HrManagementDbContext verification =
            database.CreateContext();

        PayrollPeriod savedPeriod =
            await verification
                .PayrollPeriods
                .AsNoTracking()
                .SingleAsync();

        PayrollEmployeeSnapshot savedSnapshot =
            await verification
                .PayrollEmployeeSnapshots
                .AsNoTracking()
                .SingleAsync();

        AuditEntry[] audits =
            await verification
                .AuditEntries
                .AsNoTracking()
                .OrderBy(
                    audit =>
                        audit.EntityType)
                .ToArrayAsync();

        Assert.True(
            savedPeriod.IsClosed);

        Assert.Equal(
            period.Id,
            savedPeriod.Id);

        Assert.Equal(
            snapshot.Id,
            savedSnapshot.Id);

        Assert.Equal(
            12_480_000m,
            savedSnapshot.BaseSalaryAmount);

        Assert.Equal(
            0m,
            savedSnapshot.OvertimeAmount);

        Assert.Equal(
            12_480_000m,
            savedSnapshot.GrossAmount);

        Assert.Equal(
            2,
            audits.Length);

        Assert.Contains(
            audits,
            audit =>
                audit.EntityType ==
                    AuditEntityTypes.PayrollPeriod
                && audit.EntityId ==
                    period.Id
                && audit.EmployeeId ==
                    null);

        Assert.Contains(
            audits,
            audit =>
                audit.EntityType ==
                    AuditEntityTypes.PayrollEmployeeSnapshot
                && audit.EntityId ==
                    snapshot.Id
                && audit.EmployeeId ==
                    seed.EmployeeId);

        Assert.All(
            audits,
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

    [Fact]
    public async Task PersistAsync_WhenPeriodAlreadyClosed_RejectsSecondCloseWithoutAdditionalWrites()
    {
        await using TestDatabase database =
            await TestDatabase.CreateAsync();

        SeedResult seed =
            await database.SeedAsync();

        var persistence =
            CreatePersistence(
                database);

        PayrollPeriod firstPeriod =
            CreateClosedPayrollPeriod(
                seed.TimesheetPeriodId);

        await persistence.PersistAsync(
            firstPeriod,
            [
                CreateSnapshot(
                    firstPeriod.Id,
                    seed.EmployeeId,
                    12_480_000m)
            ],
            "user-1",
            "admin");

        PayrollPeriod secondPeriod =
            CreateClosedPayrollPeriod(
                seed.TimesheetPeriodId);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () =>
                persistence.PersistAsync(
                    secondPeriod,
                    [
                        CreateSnapshot(
                            secondPeriod.Id,
                            seed.EmployeeId,
                            12_480_000m)
                    ],
                    "user-1",
                    "admin"));

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

        Assert.Equal(
            2,
            await verification
                .AuditEntries
                .CountAsync());
    }

    [Fact]
    public async Task PersistAsync_WhenCompensationChangedAfterPreview_RejectsStaleSnapshotWithoutWrites()
    {
        await using TestDatabase database =
            await TestDatabase.CreateAsync();

        SeedResult seed =
            await database.SeedAsync(
                monthlyBaseSalary:
                    24_960_000m);

        var persistence =
            CreatePersistence(
                database);

        PayrollPeriod period =
            CreateClosedPayrollPeriod(
                seed.TimesheetPeriodId);

        PayrollEmployeeSnapshot staleSnapshot =
            CreateSnapshot(
                period.Id,
                seed.EmployeeId,
                baseSalary:
                    12_480_000m);

        InvalidOperationException exception =
            await Assert.ThrowsAsync<InvalidOperationException>(
                () =>
                    persistence.PersistAsync(
                        period,
                        [staleSnapshot],
                        "user-1",
                        "admin"));

        Assert.Contains(
            "đã lỗi thời",
            exception.Message);

        await AssertNoPayrollWritesAsync(
            database);
    }

    [Fact]
    public async Task PersistAsync_WhenApprovedOvertimeChangedAfterPreview_RejectsStaleSnapshotWithoutWrites()
    {
        await using TestDatabase database =
            await TestDatabase.CreateAsync();

        SeedResult seed =
            await database.SeedAsync(
                overtimeWorkDate:
                    new DateOnly(
                        2026,
                        8,
                        10),
                overtimeWorkedMinutes:
                    120,
                approvedOvertimeMinutes:
                    120);

        var persistence =
            CreatePersistence(
                database);

        PayrollPeriod period =
            CreateClosedPayrollPeriod(
                seed.TimesheetPeriodId);

        PayrollEmployeeSnapshot staleSnapshot =
            CreateSnapshot(
                period.Id,
                seed.EmployeeId,
                baseSalary:
                    12_480_000m);

        InvalidOperationException exception =
            await Assert.ThrowsAsync<InvalidOperationException>(
                () =>
                    persistence.PersistAsync(
                        period,
                        [staleSnapshot],
                        "user-1",
                        "admin"));

        Assert.Contains(
            "đã lỗi thời",
            exception.Message);

        await AssertNoPayrollWritesAsync(
            database);
    }

    [Fact]
    public async Task PersistAsync_WhenActorDoesNotMatchPeriod_RejectsBeforeAnyWrite()
    {
        await using TestDatabase database =
            await TestDatabase.CreateAsync();

        SeedResult seed =
            await database.SeedAsync();

        var persistence =
            CreatePersistence(
                database);

        PayrollPeriod period =
            CreateClosedPayrollPeriod(
                seed.TimesheetPeriodId);

        PayrollEmployeeSnapshot snapshot =
            CreateSnapshot(
                period.Id,
                seed.EmployeeId,
                12_480_000m);

        InvalidOperationException exception =
            await Assert.ThrowsAsync<InvalidOperationException>(
                () =>
                    persistence.PersistAsync(
                        period,
                        [snapshot],
                        "user-2",
                        "other-admin"));

        Assert.Contains(
            "Actor đóng kỳ lương không khớp",
            exception.Message);

        await AssertNoPayrollWritesAsync(
            database);
    }

    private static EfClosePayrollPeriodPersistence
        CreatePersistence(
            TestDatabase database)
    {
        var baseSalaryPolicy =
            new CalendarDayBaseSalaryProrationPolicy();

        var overtimePayabilityPolicy =
            new ConservativeOvertimePayabilityPolicy();

        var overtimeResolver =
            new EmployeeOvertimePayabilityResolver(
                overtimePayabilityPolicy);

        var profile =
            new PayrollPolicyProfile(
                standardMonthlyWorkingMinutes:
                    12_480,
                nonWorkingDayOvertimeMultiplier:
                    2m);

        var profileSource =
            new ConfiguredPayrollPolicyProfileSource(
                profile);

        var overtimeAmountPolicy =
            new ConfiguredOvertimeAmountPolicy(
                profileSource);

        var moneyRoundingPolicy =
            new CurrencyMoneyRoundingPolicy();

        return new EfClosePayrollPeriodPersistence(
            database.Factory,
            baseSalaryPolicy,
            overtimeResolver,
            overtimeAmountPolicy,
            moneyRoundingPolicy);
    }

    private static PayrollPeriod CreateClosedPayrollPeriod(
        Guid timesheetPeriodId)
    {
        var period =
            new PayrollPeriod(
                Guid.NewGuid(),
                timesheetPeriodId,
                2026,
                8);

        period.Close(
            Utc(
                12),
            "user-1",
            "admin");

        return period;
    }

    private static PayrollEmployeeSnapshot CreateSnapshot(
        Guid payrollPeriodId,
        Guid employeeId,
        decimal baseSalary,
        int approvedOvertimeMinutes = 0,
        int payableOvertimeMinutes = 0,
        decimal overtimeAmount = 0m)
    {
        return new PayrollEmployeeSnapshot(
            Guid.NewGuid(),
            payrollPeriodId,
            employeeId,
            "EMP001",
            "Nguyễn Văn An",
            "VND",
            baseSalary,
            approvedOvertimeMinutes,
            payableOvertimeMinutes,
            overtimeAmount,
            baseSalary
            + overtimeAmount);
    }

    private static async Task AssertNoPayrollWritesAsync(
        TestDatabase database)
    {
        await using HrManagementDbContext verification =
            database.CreateContext();

        Assert.Equal(
            0,
            await verification
                .PayrollPeriods
                .CountAsync());

        Assert.Equal(
            0,
            await verification
                .PayrollEmployeeSnapshots
                .CountAsync());

        Assert.Equal(
            0,
            await verification
                .AuditEntries
                .CountAsync());
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
            DbContextOptions<HrManagementDbContext> options)
        {
            _connection =
                connection;

            _options =
                options;

            Factory =
                new TestDbContextFactory(
                    options);
        }

        public IDbContextFactory<HrManagementDbContext>
            Factory
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

            var database =
                new TestDatabase(
                    connection,
                    options);

            await using (
                HrManagementDbContext dbContext =
                    database.CreateContext())
            {
                await dbContext.Database
                    .EnsureCreatedAsync();
            }

            return database;
        }

        public HrManagementDbContext CreateContext()
        {
            return new HrManagementDbContext(
                _options);
        }

        public async Task<SeedResult> SeedAsync(
            decimal monthlyBaseSalary = 12_480_000m,
            DateOnly? overtimeWorkDate = null,
            int overtimeWorkedMinutes = 0,
            int? approvedOvertimeMinutes = null)
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
                    monthlyBaseSalary,
                    "VND");

            var timesheetPeriod =
                new TimesheetPeriod(
                    timesheetPeriodId,
                    2026,
                    8);

            timesheetPeriod.Close(
                Utc(
                    9),
                "user-timesheet",
                "timesheet-admin");

            var attendanceRecords =
                new List<AttendanceRecord>();

            var snapshots =
                new List<MonthlyTimesheetDaySnapshot>();

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
                    overtimeWorkDate.HasValue
                    && overtimeWorkDate.Value ==
                        workDate
                        ? overtimeWorkedMinutes
                        : 0;

                var snapshot =
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

                snapshots.Add(
                    snapshot);
            }

            OvertimeRequest? overtimeRequest =
                null;

            if (overtimeWorkDate.HasValue
                && approvedOvertimeMinutes.HasValue)
            {
                overtimeRequest =
                    new OvertimeRequest(
                        Guid.NewGuid(),
                        employeeId,
                        employmentPeriodId,
                        overtimeWorkDate.Value,
                        approvedOvertimeMinutes.Value,
                        "Integration test",
                        Utc(
                            7));

                overtimeRequest.TransitionTo(
                    Guid.NewGuid(),
                    OvertimeRequestStatus.Approved,
                    Utc(
                        8),
                    "user-ot",
                    "ot-admin",
                    approvedMinutes:
                        approvedOvertimeMinutes.Value);
            }

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
                snapshots);

            if (overtimeRequest is not null)
            {
                dbContext.OvertimeRequests.Add(
                    overtimeRequest);
            }

            await dbContext.SaveChangesAsync();

            return new SeedResult(
                employeeId,
                timesheetPeriodId);
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

        public Task<HrManagementDbContext> CreateDbContextAsync(
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
            31,
            hour,
            0,
            0,
            DateTimeKind.Utc);
    }
}
