using HrManagement.Domain.Attendance.Corrections;
using HrManagement.Domain.Attendance.Timesheets;
using HrManagement.Domain.Attendance.Calculations;
using HrManagement.Domain.Attendance.Records;
using HrManagement.Domain.Attendance.Schedules;
using HrManagement.Domain.Employees;
using HrManagement.Infrastructure.Attendance.Calculations;
using HrManagement.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace HrManagement.Tests.Attendance;

public sealed class AttendanceCalculationPersistenceTests
{
    [Fact]
    public async Task MatchingTimeline_PersistsCalculatedState()
    {
        await using SqliteConnection connection =
            await CreateOpenConnectionAsync();

        DbContextOptions<HrManagementDbContext> options =
            CreateOptions(
                connection);

        await EnsureCreatedAsync(
            options);

        SeedIds ids =
            await SeedContextAsync(
                options);

        AttendanceRecord record =
            CreateRecord(
                ids);

        AttendanceEvent clockIn =
            Event(
                record,
                AttendanceEventType.ClockIn,
                Utc(
                    8,
                    10));

        AttendanceEvent clockOut =
            Event(
                record,
                AttendanceEventType.ClockOut,
                Utc(
                    16,
                    45));

        await SeedRecordAsync(
            options,
            record,
            clockIn,
            clockOut);

        ApplyCalculation(
            record,
            [
                clockIn,
                clockOut
            ]);

        var persistence =
            CreatePersistence(
                options);

        await persistence.ApplyAsync(
            record,
            [
                clockIn,
                clockOut
            ],
            expectedCorrectionRevision: 0);

        await using var verification =
            new HrManagementDbContext(
                options);

        AttendanceRecord saved =
            await verification
                .AttendanceRecords
                .AsNoTracking()
                .SingleAsync();

        Assert.Equal(
            AttendanceCalculationStatus.Present,
            saved.Status);

        Assert.Equal(
            515,
            saved.WorkedMinutes);

        Assert.Equal(
            10,
            saved.LateMinutes);

        Assert.Equal(
            15,
            saved.EarlyLeaveMinutes);
    }

    [Fact]
    public async Task EmptyTimeline_PersistsAbsentState()
    {
        await using SqliteConnection connection =
            await CreateOpenConnectionAsync();

        DbContextOptions<HrManagementDbContext> options =
            CreateOptions(
                connection);

        await EnsureCreatedAsync(
            options);

        SeedIds ids =
            await SeedContextAsync(
                options);

        AttendanceRecord record =
            CreateRecord(
                ids);

        await SeedRecordAsync(
            options,
            record);

        ApplyCalculation(
            record,
            []);

        var persistence =
            CreatePersistence(
                options);

        await persistence.ApplyAsync(
            record,
            [],
            expectedCorrectionRevision: 0);

        await using var verification =
            new HrManagementDbContext(
                options);

        AttendanceRecord saved =
            await verification
                .AttendanceRecords
                .AsNoTracking()
                .SingleAsync();

        Assert.Equal(
            AttendanceCalculationStatus.Absent,
            saved.Status);

        Assert.Equal(
            0,
            saved.WorkedMinutes);
    }

    [Fact]
    public async Task NewCorrectionAfterCalculation_ThrowsConcurrency()
    {
        await using SqliteConnection connection =
            await CreateOpenConnectionAsync();

        DbContextOptions<HrManagementDbContext> options =
            CreateOptions(
                connection);

        await EnsureCreatedAsync(
            options);

        SeedIds ids =
            await SeedContextAsync(
                options);

        AttendanceRecord record =
            CreateRecord(
                ids);

        AttendanceEvent clockIn =
            Event(
                record,
                AttendanceEventType.ClockIn,
                Utc(
                    8,
                    0));

        AttendanceEvent clockOut =
            Event(
                record,
                AttendanceEventType.ClockOut,
                Utc(
                    17,
                    0));

        await SeedRecordAsync(
            options,
            record,
            clockIn,
            clockOut);

        IReadOnlyList<AttendanceEvent> expectedEvents =
        [
            clockIn,
        clockOut
        ];

        ApplyCalculation(
            record,
            expectedEvents);

        var correction =
            new AttendanceCorrection(
                Guid.NewGuid(),
                record.Id,
                record.EmployeeId,
                clockIn.Id,
                revision: 1,
                AttendanceCorrectionKind.ChangeEvent,
                AttendanceEventType.ClockIn,
                Utc(
                    8,
                    0),
                AttendanceEventType.ClockIn,
                Utc(
                    8,
                    15),
                "Sửa giờ chấm vào",
                Utc(
                    18,
                    0),
                "user-1",
                "admin");

        await using (
            var mutationContext =
                new HrManagementDbContext(
                    options))
        {
            await mutationContext
                .AttendanceCorrections
                .AddAsync(
                    correction);

            await mutationContext.SaveChangesAsync();
        }

        var persistence =
            CreatePersistence(
                options);

        await Assert.ThrowsAsync<
            DbUpdateConcurrencyException>(
            () =>
                persistence.ApplyAsync(
                    record,
                    expectedEvents,
                    expectedCorrectionRevision: 0));

        await using var verification =
            new HrManagementDbContext(
                options);

        AttendanceRecord saved =
            await verification
                .AttendanceRecords
                .AsNoTracking()
                .SingleAsync();

        Assert.Equal(
            AttendanceCalculationStatus.NotCalculated,
            saved.Status);

        Assert.Equal(
            0,
            saved.WorkedMinutes);

        Assert.Single(
            await verification
                .AttendanceCorrections
                .AsNoTracking()
                .ToArrayAsync());
    }

    [Fact]
    public async Task MatchingCorrectionRevision_PersistsCalculatedState()
    {
        await using SqliteConnection connection =
            await CreateOpenConnectionAsync();

        DbContextOptions<HrManagementDbContext> options =
            CreateOptions(
                connection);

        await EnsureCreatedAsync(
            options);

        SeedIds ids =
            await SeedContextAsync(
                options);

        AttendanceRecord record =
            CreateRecord(
                ids);

        AttendanceEvent clockIn =
            Event(
                record,
                AttendanceEventType.ClockIn,
                Utc(
                    8,
                    0));

        AttendanceEvent clockOut =
            Event(
                record,
                AttendanceEventType.ClockOut,
                Utc(
                    17,
                    0));

        await SeedRecordAsync(
            options,
            record,
            clockIn,
            clockOut);

        var correction =
            new AttendanceCorrection(
                Guid.NewGuid(),
                record.Id,
                record.EmployeeId,
                clockIn.Id,
                revision: 1,
                AttendanceCorrectionKind.ChangeEvent,
                AttendanceEventType.ClockIn,
                Utc(
                    8,
                    0),
                AttendanceEventType.ClockIn,
                Utc(
                    9,
                    0),
                "Sửa giờ chấm vào",
                Utc(
                    18,
                    0),
                "user-1",
                "admin");

        await using (
            var mutationContext =
                new HrManagementDbContext(
                    options))
        {
            await mutationContext
                .AttendanceCorrections
                .AddAsync(
                    correction);

            await mutationContext.SaveChangesAsync();
        }

        AttendanceEvent effectiveClockIn =
            new(
                clockIn.Id,
                record.Id,
                record.EmployeeId,
                AttendanceEventType.ClockIn,
                Utc(
                    9,
                    0));

        ApplyCalculation(
            record,
            [
                effectiveClockIn,
            clockOut
            ]);

        var persistence =
            CreatePersistence(
                options);

        await persistence.ApplyAsync(
            record,
            [
                clockIn,
            clockOut
            ],
            expectedCorrectionRevision: 1);

        await using var verification =
            new HrManagementDbContext(
                options);

        AttendanceRecord saved =
            await verification
                .AttendanceRecords
                .AsNoTracking()
                .SingleAsync();

        Assert.Equal(
            AttendanceCalculationStatus.Present,
            saved.Status);

        Assert.Equal(
            480,
            saved.WorkedMinutes);
    }

    [Fact]
    public async Task NewRawEventAfterCalculation_ThrowsConcurrency()
    {
        await using SqliteConnection connection =
            await CreateOpenConnectionAsync();

        DbContextOptions<HrManagementDbContext> options =
            CreateOptions(
                connection);

        await EnsureCreatedAsync(
            options);

        SeedIds ids =
            await SeedContextAsync(
                options);

        AttendanceRecord record =
            CreateRecord(
                ids);

        AttendanceEvent clockIn =
            Event(
                record,
                AttendanceEventType.ClockIn,
                Utc(
                    8,
                    0));

        AttendanceEvent clockOut =
            Event(
                record,
                AttendanceEventType.ClockOut,
                Utc(
                    12,
                    0));

        await SeedRecordAsync(
            options,
            record,
            clockIn,
            clockOut);

        IReadOnlyList<AttendanceEvent> expectedEvents =
        [
            clockIn,
            clockOut
        ];

        ApplyCalculation(
            record,
            expectedEvents);

        AttendanceEvent newClockIn =
            Event(
                record,
                AttendanceEventType.ClockIn,
                Utc(
                    13,
                    0));

        await using (
            var mutationContext =
                new HrManagementDbContext(
                    options))
        {
            await mutationContext
                .AttendanceEvents
                .AddAsync(
                    newClockIn);

            await mutationContext.SaveChangesAsync();
        }

        var persistence =
            CreatePersistence(
                options);

        await Assert.ThrowsAsync<
            DbUpdateConcurrencyException>(
            () =>
                persistence.ApplyAsync(
                    record,
                    expectedEvents,
                    expectedCorrectionRevision: 0));

        await using var verification =
            new HrManagementDbContext(
                options);

        AttendanceRecord saved =
            await verification
                .AttendanceRecords
                .AsNoTracking()
                .SingleAsync();

        Assert.Equal(
            AttendanceCalculationStatus.NotCalculated,
            saved.Status);

        Assert.Equal(
            0,
            saved.WorkedMinutes);

        Assert.Equal(
            3,
            await verification
                .AttendanceEvents
                .CountAsync());
    }

    [Fact]
    public async Task ExpectedEventPayloadMismatch_ThrowsConcurrency()
    {
        await using SqliteConnection connection =
            await CreateOpenConnectionAsync();

        DbContextOptions<HrManagementDbContext> options =
            CreateOptions(
                connection);

        await EnsureCreatedAsync(
            options);

        SeedIds ids =
            await SeedContextAsync(
                options);

        AttendanceRecord record =
            CreateRecord(
                ids);

        AttendanceEvent clockIn =
            Event(
                record,
                AttendanceEventType.ClockIn,
                Utc(
                    8,
                    0));

        AttendanceEvent clockOut =
            Event(
                record,
                AttendanceEventType.ClockOut,
                Utc(
                    17,
                    0));

        await SeedRecordAsync(
            options,
            record,
            clockIn,
            clockOut);

        AttendanceEvent staleClockOut =
            new(
                clockOut.Id,
                clockOut.AttendanceRecordId,
                clockOut.EmployeeId,
                clockOut.EventType,
                Utc(
                    16,
                    59));

        IReadOnlyList<AttendanceEvent> staleEvents =
        [
            clockIn,
            staleClockOut
        ];

        ApplyCalculation(
            record,
            staleEvents);

        var persistence =
            CreatePersistence(
                options);

        await Assert.ThrowsAsync<
            DbUpdateConcurrencyException>(
            () =>
                persistence.ApplyAsync(
                    record,
                    staleEvents,
                    expectedCorrectionRevision: 0));
    }

    [Fact]
    public async Task MissingRecord_ThrowsConcurrency()
    {
        await using SqliteConnection connection =
            await CreateOpenConnectionAsync();

        DbContextOptions<HrManagementDbContext> options =
            CreateOptions(
                connection);

        await EnsureCreatedAsync(
            options);

        SeedIds ids =
            await SeedContextAsync(
                options);

        AttendanceRecord record =
            CreateRecord(
                ids);

        ApplyCalculation(
            record,
            []);

        var persistence =
            CreatePersistence(
                options);

        await Assert.ThrowsAsync<
            DbUpdateConcurrencyException>(
            () =>
                persistence.ApplyAsync(
                    record,
                    [],
                    expectedCorrectionRevision: 0));
    }

    [Fact]
    public async Task ChangedScheduleSnapshot_ThrowsConcurrency()
    {
        await using SqliteConnection connection =
            await CreateOpenConnectionAsync();

        DbContextOptions<HrManagementDbContext> options =
            CreateOptions(
                connection);

        await EnsureCreatedAsync(
            options);

        SeedIds ids =
            await SeedContextAsync(
                options);

        AttendanceRecord persistedRecord =
            CreateRecord(
                ids);

        await SeedRecordAsync(
            options,
            persistedRecord);

        var staleRecord =
            new AttendanceRecord(
                persistedRecord.Id,
                persistedRecord.EmployeeId,
                persistedRecord.EmploymentPeriodId,
                persistedRecord.WorkScheduleAssignmentId,
                persistedRecord.WorkScheduleId,
                persistedRecord.WorkDate,
                "UTC",
                true,
                new TimeOnly(
                    8,
                    0),
                new TimeOnly(
                    17,
                    0),
                60);

        ApplyCalculation(
            staleRecord,
            []);

        var persistence =
            CreatePersistence(
                options);

        await Assert.ThrowsAsync<
            DbUpdateConcurrencyException>(
            () =>
                persistence.ApplyAsync(
                    staleRecord,
                    [],
                    expectedCorrectionRevision: 0));

        await using var verification =
            new HrManagementDbContext(
                options);

        AttendanceRecord saved =
            await verification
                .AttendanceRecords
                .AsNoTracking()
                .SingleAsync();

        Assert.Equal(
            AttendanceCalculationStatus.NotCalculated,
            saved.Status);
    }

    [Fact]
    public async Task ApplyAsync_WhenPeriodIsClosed_RejectsWithoutUpdatingCalculation()
    {
        await using SqliteConnection connection =
            await CreateOpenConnectionAsync();

        DbContextOptions<HrManagementDbContext> options =
            CreateOptions(
                connection);

        await EnsureCreatedAsync(
            options);

        SeedIds ids =
            await SeedContextAsync(
                options);

        AttendanceRecord record =
            CreateRecord(
                ids);

        await SeedRecordAsync(
            options,
            record);

        ApplyCalculation(
            record,
            []);

        var period =
            new TimesheetPeriod(
                Guid.NewGuid(),
                2026,
                8);

        period.Close(
            new DateTime(
                2026,
                8,
                31,
                12,
                0,
                0,
                DateTimeKind.Utc),
            "user-1",
            "admin");

        await using (
            var mutationContext =
                new HrManagementDbContext(
                    options))
        {
            mutationContext.TimesheetPeriods.Add(
                period);

            await mutationContext.SaveChangesAsync();
        }

        EfAttendanceCalculationPersistence persistence =
            CreatePersistence(
                options);

        InvalidOperationException exception =
            await Assert.ThrowsAsync<InvalidOperationException>(
                () =>
                    persistence.ApplyAsync(
                        record,
                        [],
                        expectedCorrectionRevision: 0));

        Assert.Equal(
            "Kỳ công của ngày chấm công đã được đóng. Không thể thay đổi dữ liệu chấm công.",
            exception.Message);

        await using var verification =
            new HrManagementDbContext(
                options);

        AttendanceRecord saved =
            await verification
                .AttendanceRecords
                .AsNoTracking()
                .SingleAsync();

        Assert.Equal(
            AttendanceCalculationStatus.NotCalculated,
            saved.Status);

        Assert.Equal(
            0,
            saved.WorkedMinutes);

        Assert.Equal(
            0,
            saved.LateMinutes);

        Assert.Equal(
            0,
            saved.EarlyLeaveMinutes);

        TimesheetPeriod persistedPeriod =
            await verification
                .TimesheetPeriods
                .AsNoTracking()
                .SingleAsync();

        Assert.True(
            persistedPeriod.IsClosed);
    }

    private static void ApplyCalculation(
        AttendanceRecord record,
        IReadOnlyList<AttendanceEvent> events)
    {
        DailyAttendanceCalculation daily =
            DailyAttendanceCalculator.Calculate(
                record,
                events);

        AttendanceScheduleWindow? window =
            record.IsWorkingDay
                ? new AttendanceScheduleWindow(
                    Utc(
                        8,
                        0),
                    Utc(
                        17,
                        0))
                : null;

        AttendanceScheduleAdherence adherence =
            AttendanceScheduleAdherenceCalculator.Calculate(
                record,
                daily,
                window,
                new AttendanceAdherencePolicy());

        record.ApplyCalculation(
            daily,
            adherence);
    }

    private static EfAttendanceCalculationPersistence
        CreatePersistence(
            DbContextOptions<HrManagementDbContext> options)
    {
        return new EfAttendanceCalculationPersistence(
            new TestDbContextFactory(
                options));
    }

    private static AttendanceRecord CreateRecord(
        SeedIds ids)
    {
        return new AttendanceRecord(
            Guid.NewGuid(),
            ids.EmployeeId,
            ids.EmploymentPeriodId,
            ids.AssignmentId,
            ids.ScheduleId,
            new DateOnly(
                2026,
                8,
                20),
            "SE Asia Standard Time",
            true,
            new TimeOnly(
                8,
                0),
            new TimeOnly(
                17,
                0),
            60);
    }

    private static AttendanceEvent Event(
        AttendanceRecord record,
        AttendanceEventType eventType,
        DateTime occurredAtUtc)
    {
        return new AttendanceEvent(
            Guid.NewGuid(),
            record.Id,
            record.EmployeeId,
            eventType,
            occurredAtUtc);
    }

    private static async Task SeedRecordAsync(
        DbContextOptions<HrManagementDbContext> options,
        AttendanceRecord record,
        params AttendanceEvent[] events)
    {
        await using var dbContext =
            new HrManagementDbContext(
                options);

        await dbContext
            .AttendanceRecords
            .AddAsync(
                record);

        if (events.Length > 0)
        {
            await dbContext
                .AttendanceEvents
                .AddRangeAsync(
                    events);
        }

        await dbContext.SaveChangesAsync();
    }

    private static async Task<SeedIds> SeedContextAsync(
        DbContextOptions<HrManagementDbContext> options)
    {
        var ids =
            new SeedIds(
                Guid.NewGuid(),
                Guid.NewGuid(),
                Guid.NewGuid(),
                Guid.NewGuid());

        await using var dbContext =
            new HrManagementDbContext(
                options);

        await dbContext.Employees.AddAsync(
            new Employee(
                ids.EmployeeId,
                $"EMP{ids.EmployeeId:N}"[..20],
                "Nhân viên kiểm thử",
                null,
                null,
                null,
                new DateOnly(
                    2025,
                    1,
                    1),
                "Phòng kiểm thử",
                "Chuyên viên kiểm thử",
                EmployeeStatus.Active));

        await dbContext.EmploymentPeriods.AddAsync(
            new EmploymentPeriod(
                ids.EmploymentPeriodId,
                ids.EmployeeId,
                new DateOnly(
                    2026,
                    1,
                    1)));

        await dbContext.WorkSchedules.AddAsync(
            new WorkSchedule(
                ids.ScheduleId,
                $"S{ids.ScheduleId:N}"[..20],
                "Lịch kiểm thử",
                "SE Asia Standard Time"));

        await dbContext.EmployeeWorkScheduleAssignments.AddAsync(
            new EmployeeWorkScheduleAssignment(
                ids.AssignmentId,
                ids.EmployeeId,
                ids.EmploymentPeriodId,
                ids.ScheduleId,
                new DateOnly(
                    2026,
                    1,
                    1)));

        await dbContext.SaveChangesAsync();

        return ids;
    }

    private static DateTime Utc(
        int hour,
        int minute)
    {
        return new DateTime(
            2026,
            8,
            20,
            hour,
            minute,
            0,
            DateTimeKind.Utc);
    }

    private static async Task<SqliteConnection>
        CreateOpenConnectionAsync()
    {
        var connection =
            new SqliteConnection(
                "Data Source=:memory:;Foreign Keys=True");

        await connection.OpenAsync();

        return connection;
    }

    private static DbContextOptions<HrManagementDbContext>
        CreateOptions(
            SqliteConnection connection)
    {
        return new DbContextOptionsBuilder<
                HrManagementDbContext>()
            .UseSqlite(
                connection)
            .Options;
    }

    private static async Task EnsureCreatedAsync(
        DbContextOptions<HrManagementDbContext> options)
    {
        await using var dbContext =
            new HrManagementDbContext(
                options);

        await dbContext.Database
            .EnsureCreatedAsync();
    }

    private sealed record SeedIds(
        Guid EmployeeId,
        Guid EmploymentPeriodId,
        Guid ScheduleId,
        Guid AssignmentId);

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
