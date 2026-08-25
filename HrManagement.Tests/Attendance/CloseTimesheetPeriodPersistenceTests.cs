using HrManagement.Application.Attendance.Timesheets;
using HrManagement.Application.Auditing;
using HrManagement.Domain.Attendance.Calculations;
using HrManagement.Domain.Attendance.Corrections;
using HrManagement.Domain.Attendance.Records;
using HrManagement.Domain.Attendance.Schedules;
using HrManagement.Domain.Attendance.Timesheets;
using HrManagement.Domain.Auditing;
using HrManagement.Domain.Employees;
using HrManagement.Infrastructure.Attendance.Timesheets;
using HrManagement.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace HrManagement.Tests.Attendance;

public sealed class CloseTimesheetPeriodPersistenceTests
{
    [Fact]
    public async Task CloseAsync_WhenMonthIsComplete_PersistsPeriodSnapshotAndAudit()
    {
        await using TestDatabase database =
            await TestDatabase.CreateAsync();

        SeedResult seed =
            await database.SeedCompleteAttendanceAsync(
                correctionRevisionCount: 3);

        DateTime closedAtUtc =
            Utc(
                18);

        CloseTimesheetPeriodPersistenceResult result =
            await database.Persistence.CloseAsync(
                2026,
                8,
                closedAtUtc,
                "user-1",
                "admin");

        Assert.Equal(
            1,
            result.SnapshotCount);

        await using HrManagementDbContext dbContext =
            await database.Factory.CreateDbContextAsync();

        TimesheetPeriod period =
            await dbContext
                .TimesheetPeriods
                .AsNoTracking()
                .SingleAsync();

        Assert.Equal(
            result.TimesheetPeriodId,
            period.Id);

        Assert.True(
            period.IsClosed);

        Assert.Equal(
            TimesheetPeriodStatus.Closed,
            period.Status);

        Assert.Equal(
            closedAtUtc,
            period.ClosedAtUtc);

        Assert.Equal(
            "user-1",
            period.ClosedByUserId);

        Assert.Equal(
            "admin",
            period.ClosedByUsername);

        MonthlyTimesheetDaySnapshot snapshot =
            await dbContext
                .MonthlyTimesheetDaySnapshots
                .AsNoTracking()
                .SingleAsync();

        Assert.Equal(
            period.Id,
            snapshot.TimesheetPeriodId);

        Assert.Equal(
            seed.AttendanceRecordId,
            snapshot.AttendanceRecordId);

        Assert.Equal(
            seed.EmployeeId,
            snapshot.EmployeeId);

        Assert.Equal(
            seed.WorkDate,
            snapshot.WorkDate);

        Assert.True(
            snapshot.IsWorkingDay);

        Assert.Equal(
            480,
            snapshot.ExpectedPlannedMinutes);

        Assert.Equal(
            AttendanceCalculationStatus.Absent,
            snapshot.Status);

        Assert.Equal(
            0,
            snapshot.WorkedMinutes);

        Assert.Equal(
            0,
            snapshot.LateMinutes);

        Assert.Equal(
            0,
            snapshot.EarlyLeaveMinutes);

        Assert.Equal(
            3,
            snapshot.CorrectionRevision);

        AuditEntry audit =
            await dbContext
                .AuditEntries
                .AsNoTracking()
                .SingleAsync();

        Assert.Equal(
            AuditAction.Updated,
            audit.Action);

        Assert.Equal(
            AuditEntityTypes.TimesheetPeriod,
            audit.EntityType);

        Assert.Equal(
            period.Id,
            audit.EntityId);

        Assert.Null(
            audit.EmployeeId);

        Assert.Equal(
            closedAtUtc,
            audit.OccurredAtUtc);

        Assert.Equal(
            "user-1",
            audit.ActorUserId);

        Assert.Equal(
            "admin",
            audit.ActorUsername);
    }

    [Fact]
    public async Task CloseAsync_WhenAttendanceIsMissing_RollsBackEverything()
    {
        await using TestDatabase database =
            await TestDatabase.CreateAsync();

        await database.SeedCoverageOnlyAsync();

        InvalidOperationException exception =
            await Assert.ThrowsAsync<InvalidOperationException>(
                () =>
                    database.Persistence.CloseAsync(
                        2026,
                        8,
                        Utc(
                            18),
                        "user-1",
                        "admin"));

        Assert.Equal(
            "Không thể đóng kỳ công vì dữ liệu chấm công trong tháng chưa được sinh đầy đủ.",
            exception.Message);

        await AssertNoTimesheetWritesAsync(
            database);
    }

    [Fact]
    public async Task CloseAsync_WhenAttendanceIsNotCalculated_RollsBackEverything()
    {
        await using TestDatabase database =
            await TestDatabase.CreateAsync();

        await database.SeedUnresolvedAttendanceAsync();

        InvalidOperationException exception =
            await Assert.ThrowsAsync<InvalidOperationException>(
                () =>
                    database.Persistence.CloseAsync(
                        2026,
                        8,
                        Utc(
                            18),
                        "user-1",
                        "admin"));

        Assert.Equal(
            "Không thể đóng kỳ công vì vẫn còn bản ghi chấm công chưa được tính hoàn tất.",
            exception.Message);

        await AssertNoTimesheetWritesAsync(
            database);
    }

    [Fact]
    public async Task CloseAsync_WhenCalledTwice_DoesNotCreateDuplicates()
    {
        await using TestDatabase database =
            await TestDatabase.CreateAsync();

        await database.SeedCompleteAttendanceAsync(
            correctionRevisionCount: 0);

        CloseTimesheetPeriodPersistenceResult first =
            await database.Persistence.CloseAsync(
                2026,
                8,
                Utc(
                    18),
                "user-1",
                "admin");

        InvalidOperationException exception =
            await Assert.ThrowsAsync<InvalidOperationException>(
                () =>
                    database.Persistence.CloseAsync(
                        2026,
                        8,
                        Utc(
                            19),
                        "user-1",
                        "admin"));

        Assert.Equal(
            "Kỳ công đã được đóng.",
            exception.Message);

        await using HrManagementDbContext dbContext =
            await database.Factory.CreateDbContextAsync();

        Assert.Single(
            await dbContext
                .TimesheetPeriods
                .AsNoTracking()
                .ToArrayAsync());

        MonthlyTimesheetDaySnapshot snapshot =
            Assert.Single(
                await dbContext
                    .MonthlyTimesheetDaySnapshots
                    .AsNoTracking()
                    .ToArrayAsync());

        Assert.Equal(
            first.TimesheetPeriodId,
            snapshot.TimesheetPeriodId);

        Assert.Single(
            await dbContext
                .AuditEntries
                .AsNoTracking()
                .ToArrayAsync());
    }

    [Fact]
    public async Task CloseAsync_CorrectionAddedAfterClose_DoesNotChangeSnapshotRevision()
    {
        await using TestDatabase database =
            await TestDatabase.CreateAsync();

        SeedResult seed =
            await database.SeedCompleteAttendanceAsync(
                correctionRevisionCount: 3);

        await database.Persistence.CloseAsync(
            2026,
            8,
            Utc(
                18),
            "user-1",
            "admin");

        await database.AddCorrectionAsync(
            seed,
            revision: 4);

        await using HrManagementDbContext dbContext =
            await database.Factory.CreateDbContextAsync();

        int liveRevision =
            await dbContext
                .AttendanceCorrections
                .AsNoTracking()
                .Where(
                    correction =>
                        correction.AttendanceRecordId ==
                        seed.AttendanceRecordId)
                .MaxAsync(
                    correction =>
                        correction.Revision);

        MonthlyTimesheetDaySnapshot snapshot =
            await dbContext
                .MonthlyTimesheetDaySnapshots
                .AsNoTracking()
                .SingleAsync();

        Assert.Equal(
            4,
            liveRevision);

        Assert.Equal(
            3,
            snapshot.CorrectionRevision);
    }

    [Fact]
    public async Task CloseAsync_WhenMonthHasNoEmployment_ClosesEmptyPeriod()
    {
        await using TestDatabase database =
            await TestDatabase.CreateAsync();

        CloseTimesheetPeriodPersistenceResult result =
            await database.Persistence.CloseAsync(
                2026,
                8,
                Utc(
                    18),
                "user-1",
                "admin");

        Assert.Equal(
            0,
            result.SnapshotCount);

        await using HrManagementDbContext dbContext =
            await database.Factory.CreateDbContextAsync();

        TimesheetPeriod period =
            await dbContext
                .TimesheetPeriods
                .AsNoTracking()
                .SingleAsync();

        Assert.True(
            period.IsClosed);

        Assert.Equal(
            result.TimesheetPeriodId,
            period.Id);

        Assert.Empty(
            await dbContext
                .MonthlyTimesheetDaySnapshots
                .AsNoTracking()
                .ToArrayAsync());

        AuditEntry audit =
            await dbContext
                .AuditEntries
                .AsNoTracking()
                .SingleAsync();

        Assert.Equal(
            AuditEntityTypes.TimesheetPeriod,
            audit.EntityType);

        Assert.Equal(
            period.Id,
            audit.EntityId);
    }

    private static async Task AssertNoTimesheetWritesAsync(
        TestDatabase database)
    {
        await using HrManagementDbContext dbContext =
            await database.Factory.CreateDbContextAsync();

        Assert.Empty(
            await dbContext
                .TimesheetPeriods
                .AsNoTracking()
                .ToArrayAsync());

        Assert.Empty(
            await dbContext
                .MonthlyTimesheetDaySnapshots
                .AsNoTracking()
                .ToArrayAsync());

        Assert.Empty(
            await dbContext
                .AuditEntries
                .AsNoTracking()
                .ToArrayAsync());
    }

    private sealed record SeedResult(
        Guid EmployeeId,
        Guid EmploymentPeriodId,
        Guid AssignmentId,
        Guid WorkScheduleId,
        Guid AttendanceRecordId,
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

        public EfCloseTimesheetPeriodPersistence Persistence
        {
            get;
        }

        private TestDatabase(
            SqliteConnection connection,
            TestDbContextFactory factory)
        {
            _connection =
                connection;

            Factory =
                factory;

            Persistence =
                new EfCloseTimesheetPeriodPersistence(
                    factory);
        }

        public static async Task<TestDatabase> CreateAsync()
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
                await factory.CreateDbContextAsync();

            await dbContext.Database
                .EnsureCreatedAsync();

            return new TestDatabase(
                connection,
                factory);
        }

        public async Task SeedCoverageOnlyAsync()
        {
            DateOnly workDate =
                WorkDate();

            Guid employeeId =
                Guid.NewGuid();

            Guid employmentPeriodId =
                Guid.NewGuid();

            Guid assignmentId =
                Guid.NewGuid();

            Guid workScheduleId =
                Guid.NewGuid();

            var employmentPeriod =
                new EmploymentPeriod(
                    employmentPeriodId,
                    employeeId,
                    workDate,
                    workDate);

            var assignment =
                new EmployeeWorkScheduleAssignment(
                    assignmentId,
                    employeeId,
                    employmentPeriodId,
                    workScheduleId,
                    workDate,
                    workDate);

            await using HrManagementDbContext dbContext =
                await Factory.CreateDbContextAsync();

            dbContext.EmploymentPeriods.Add(
                employmentPeriod);

            dbContext.EmployeeWorkScheduleAssignments.Add(
                assignment);

            await dbContext.SaveChangesAsync();
        }

        public async Task<SeedResult>
            SeedUnresolvedAttendanceAsync()
        {
            return await SeedAttendanceAsync(
                finalizeAttendance: false,
                correctionRevisionCount: 0);
        }

        public async Task<SeedResult>
            SeedCompleteAttendanceAsync(
                int correctionRevisionCount)
        {
            return await SeedAttendanceAsync(
                finalizeAttendance: true,
                correctionRevisionCount);
        }

        private async Task<SeedResult>
            SeedAttendanceAsync(
                bool finalizeAttendance,
                int correctionRevisionCount)
        {
            DateOnly workDate =
                WorkDate();

            Guid employeeId =
                Guid.NewGuid();

            Guid employmentPeriodId =
                Guid.NewGuid();

            Guid assignmentId =
                Guid.NewGuid();

            Guid workScheduleId =
                Guid.NewGuid();

            Guid attendanceRecordId =
                Guid.NewGuid();

            var employmentPeriod =
                new EmploymentPeriod(
                    employmentPeriodId,
                    employeeId,
                    workDate,
                    workDate);

            var assignment =
                new EmployeeWorkScheduleAssignment(
                    assignmentId,
                    employeeId,
                    employmentPeriodId,
                    workScheduleId,
                    workDate,
                    workDate);

            var record =
                new AttendanceRecord(
                    attendanceRecordId,
                    employeeId,
                    employmentPeriodId,
                    assignmentId,
                    workScheduleId,
                    workDate,
                    "SE Asia Standard Time",
                    isWorkingDay: true,
                    expectedStartTime:
                        new TimeOnly(
                            8,
                            0),
                    expectedEndTime:
                        new TimeOnly(
                            17,
                            0),
                    expectedBreakMinutes: 60);

            if (finalizeAttendance)
            {
                DailyAttendanceCalculation calculation =
                    DailyAttendanceCalculator.Calculate(
                        record,
                        []);

                AttendanceScheduleAdherence adherence =
                    AttendanceScheduleAdherenceCalculator.Calculate(
                        record,
                        calculation,
                        scheduleWindow: null,
                        new AttendanceAdherencePolicy());

                record.ApplyCalculation(
                    calculation,
                    adherence);
            }

            await using (
                HrManagementDbContext dbContext =
                    await Factory.CreateDbContextAsync())
            {
                dbContext.EmploymentPeriods.Add(
                    employmentPeriod);

                dbContext.EmployeeWorkScheduleAssignments.Add(
                    assignment);

                dbContext.AttendanceRecords.Add(
                    record);

                await dbContext.SaveChangesAsync();
            }

            var seed =
                new SeedResult(
                    employeeId,
                    employmentPeriodId,
                    assignmentId,
                    workScheduleId,
                    attendanceRecordId,
                    workDate);

            for (int revision = 1;
                 revision <= correctionRevisionCount;
                 revision++)
            {
                await AddCorrectionAsync(
                    seed,
                    revision);
            }

            return seed;
        }

        public async Task AddCorrectionAsync(
            SeedResult seed,
            int revision)
        {
            var correction =
                new AttendanceCorrection(
                    Guid.NewGuid(),
                    seed.AttendanceRecordId,
                    seed.EmployeeId,
                    Guid.NewGuid(),
                    revision,
                    AttendanceCorrectionKind.AddEvent,
                    beforeEventType: null,
                    beforeOccurredAtUtc: null,
                    afterEventType:
                        AttendanceEventType.ClockIn,
                    afterOccurredAtUtc:
                        Utc(
                            8,
                            revision),
                    reason:
                        $"Integration correction {revision}",
                    correctedAtUtc:
                        Utc(
                            12,
                            revision),
                    actorUserId:
                        "user-1",
                    actorUsername:
                        "admin");

            await using HrManagementDbContext dbContext =
                await Factory.CreateDbContextAsync();

            dbContext.AttendanceCorrections.Add(
                correction);

            await dbContext.SaveChangesAsync();
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

    private static DateOnly WorkDate()
    {
        return new DateOnly(
            2026,
            8,
            24);
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
}
