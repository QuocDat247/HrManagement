using HrManagement.Application.Attendance.Timesheets;
using HrManagement.Domain.Attendance.Calculations;
using HrManagement.Domain.Attendance.Corrections;
using HrManagement.Domain.Attendance.Records;
using HrManagement.Domain.Attendance.Timesheets;
using HrManagement.Infrastructure.Attendance.Timesheets;
using HrManagement.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace HrManagement.Tests.Attendance;

public sealed class MonthlyTimesheetQueryIntegrationTests
{
    [Fact]
    public async Task GetLiveItemsAsync_ReturnsLatestCorrectionRevision()
    {
        await using TestDatabase database =
            await TestDatabase.CreateAsync();

        SeedResult seed =
            await database.SeedAttendanceRecordAsync();

        await database.SeedCorrectionsAsync(
            seed,
            revisionCount: 3);

        IReadOnlyList<MonthlyTimesheetDayItem> result =
            await database.Source.GetLiveItemsAsync(
                new DateOnly(
                    2026,
                    8,
                    1),
                new DateOnly(
                    2026,
                    8,
                    31));

        MonthlyTimesheetDayItem item =
            Assert.Single(
                result);

        Assert.Equal(
            seed.AttendanceRecordId,
            item.AttendanceRecordId);

        Assert.Equal(
            seed.EmployeeId,
            item.EmployeeId);

        Assert.Equal(
            seed.WorkDate,
            item.WorkDate);

        Assert.Equal(
            AttendanceCalculationStatus.Absent,
            item.Status);

        Assert.Equal(
            480,
            item.ExpectedPlannedMinutes);

        Assert.Equal(
            0,
            item.WorkedMinutes);

        Assert.Equal(
            3,
            item.CorrectionRevision);
    }

    [Fact]
    public async Task GetAsync_WhenPeriodIsClosed_ReturnsPersistedSnapshot()
    {
        await using TestDatabase database =
            await TestDatabase.CreateAsync();

        SeedResult seed =
            await database.SeedAttendanceRecordAsync();

        await database.SeedCorrectionsAsync(
            seed,
            revisionCount: 3);

        TimesheetPeriod period =
            await database.SeedClosedPeriodAsync(
                seed,
                correctionRevision: 3);

        var service =
            new MonthlyTimesheetQueryService(
                database.Source);

        MonthlyTimesheetReadModel result =
            await service.GetAsync(
                2026,
                8);

        Assert.True(
            result.IsClosed);

        Assert.Equal(
            TimesheetPeriodStatus.Closed,
            result.PeriodStatus);

        Assert.Equal(
            period.Id,
            result.TimesheetPeriodId);

        MonthlyTimesheetDayItem item =
            Assert.Single(
                result.Items);

        Assert.Equal(
            seed.AttendanceRecordId,
            item.AttendanceRecordId);

        Assert.Equal(
            AttendanceCalculationStatus.Absent,
            item.Status);

        Assert.Equal(
            480,
            item.ExpectedPlannedMinutes);

        Assert.Equal(
            3,
            item.CorrectionRevision);
    }

    [Fact]
    public async Task GetAsync_WhenClosed_LiveCorrectionAdvances_DoesNotDrift()
    {
        await using TestDatabase database =
            await TestDatabase.CreateAsync();

        SeedResult seed =
            await database.SeedAttendanceRecordAsync();

        await database.SeedCorrectionsAsync(
            seed,
            revisionCount: 3);

        await database.SeedClosedPeriodAsync(
            seed,
            correctionRevision: 3);

        var service =
            new MonthlyTimesheetQueryService(
                database.Source);

        MonthlyTimesheetReadModel beforeLiveChange =
            await service.GetAsync(
                2026,
                8);

        MonthlyTimesheetDayItem beforeItem =
            Assert.Single(
                beforeLiveChange.Items);

        await database.AddCorrectionAsync(
            seed,
            revision: 4);

        IReadOnlyList<MonthlyTimesheetDayItem>
            liveItemsAfterChange =
                await database.Source.GetLiveItemsAsync(
                    new DateOnly(
                        2026,
                        8,
                        1),
                    new DateOnly(
                        2026,
                        8,
                        31));

        MonthlyTimesheetDayItem liveItem =
            Assert.Single(
                liveItemsAfterChange);

        Assert.Equal(
            4,
            liveItem.CorrectionRevision);

        MonthlyTimesheetReadModel afterLiveChange =
            await service.GetAsync(
                2026,
                8);

        MonthlyTimesheetDayItem afterItem =
            Assert.Single(
                afterLiveChange.Items);

        Assert.True(
            afterLiveChange.IsClosed);

        Assert.Equal(
            3,
            afterItem.CorrectionRevision);

        Assert.Equal(
            beforeItem,
            afterItem);
    }

    private sealed record SeedResult(
        Guid AttendanceRecordId,
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

        public EfMonthlyTimesheetQuerySource Source
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

            Source =
                new EfMonthlyTimesheetQuerySource(
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

        public async Task<SeedResult>
            SeedAttendanceRecordAsync()
        {
            Guid attendanceRecordId =
                Guid.NewGuid();

            Guid employeeId =
                Guid.NewGuid();

            DateOnly workDate =
                new(
                    2026,
                    8,
                    24);

            var record =
                new AttendanceRecord(
                    attendanceRecordId,
                    employeeId,
                    Guid.NewGuid(),
                    Guid.NewGuid(),
                    Guid.NewGuid(),
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

            await using HrManagementDbContext dbContext =
                await Factory.CreateDbContextAsync();

            dbContext.AttendanceRecords.Add(
                record);

            await dbContext.SaveChangesAsync();

            return new SeedResult(
                attendanceRecordId,
                employeeId,
                workDate);
        }

        public async Task SeedCorrectionsAsync(
            SeedResult seed,
            int revisionCount)
        {
            for (int revision = 1;
                 revision <= revisionCount;
                 revision++)
            {
                await AddCorrectionAsync(
                    seed,
                    revision);
            }
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
                        "integration-user",
                    actorUsername:
                        "integration-admin");

            await using HrManagementDbContext dbContext =
                await Factory.CreateDbContextAsync();

            dbContext.AttendanceCorrections.Add(
                correction);

            await dbContext.SaveChangesAsync();
        }

        public async Task<TimesheetPeriod>
            SeedClosedPeriodAsync(
                SeedResult seed,
                int correctionRevision)
        {
            var period =
                new TimesheetPeriod(
                    Guid.NewGuid(),
                    2026,
                    8);

            period.Close(
                Utc(
                    13),
                "integration-user",
                "integration-admin");

            var snapshot =
                new MonthlyTimesheetDaySnapshot(
                    Guid.NewGuid(),
                    period.Id,
                    seed.AttendanceRecordId,
                    seed.EmployeeId,
                    seed.WorkDate,
                    isWorkingDay: true,
                    expectedPlannedMinutes: 480,
                    AttendanceCalculationStatus.Absent,
                    workedMinutes: 0,
                    lateMinutes: 0,
                    earlyLeaveMinutes: 0,
                    correctionRevision);

            await using HrManagementDbContext dbContext =
                await Factory.CreateDbContextAsync();

            dbContext.TimesheetPeriods.Add(
                period);

            dbContext.MonthlyTimesheetDaySnapshots.Add(
                snapshot);

            await dbContext.SaveChangesAsync();

            return period;
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
