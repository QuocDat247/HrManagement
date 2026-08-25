using HrManagement.Domain.Attendance.Timesheets;
using HrManagement.Application.Attendance.Records;
using HrManagement.Domain.Attendance.Records;
using HrManagement.Domain.Attendance.Schedules;
using HrManagement.Domain.Employees;
using HrManagement.Infrastructure.Attendance.Records;
using HrManagement.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace HrManagement.Tests.Attendance;

public sealed class AttendancePunchPersistenceTests
{
    [Fact]
    public async Task FirstPunch_CreatesRecordAndEvent()
    {
        await using SqliteConnection connection =
            await CreateOpenConnectionAsync();

        DbContextOptions<HrManagementDbContext> options =
            CreateOptions(
                connection);

        await EnsureCreatedAsync(
            options);

        SeedIds ids =
            await SeedScheduleContextAsync(
                options);

        AttendanceRecord record =
            CreateRecord(
                ids,
                Guid.NewGuid(),
                new DateOnly(
                    2026,
                    8,
                    20));

        AttendanceEvent clockIn =
            CreateEvent(
                record,
                Guid.NewGuid(),
                AttendanceEventType.ClockIn,
                Utc(
                    2026,
                    8,
                    20,
                    1,
                    0));

        var persistence =
            CreatePersistence(
                options);

        await persistence.AppendAsync(
            record,
            clockIn,
            expectedLastEvent: null);

        await using var verificationContext =
            new HrManagementDbContext(
                options);

        AttendanceRecord savedRecord =
            await verificationContext
                .AttendanceRecords
                .AsNoTracking()
                .SingleAsync();

        AttendanceEvent savedEvent =
            await verificationContext
                .AttendanceEvents
                .AsNoTracking()
                .SingleAsync();

        Assert.Equal(
            record.Id,
            savedRecord.Id);

        Assert.Equal(
            clockIn.Id,
            savedEvent.Id);

        Assert.Equal(
            record.Id,
            savedEvent.AttendanceRecordId);

        Assert.Equal(
            AttendanceEventType.ClockIn,
            savedEvent.EventType);
    }

    [Fact]
    public async Task ExistingRecord_WithExpectedClockIn_AppendsClockOut()
    {
        await using SqliteConnection connection =
            await CreateOpenConnectionAsync();

        DbContextOptions<HrManagementDbContext> options =
            CreateOptions(
                connection);

        await EnsureCreatedAsync(
            options);

        SeedIds ids =
            await SeedScheduleContextAsync(
                options);

        AttendanceRecord record =
            CreateRecord(
                ids,
                Guid.NewGuid(),
                new DateOnly(
                    2026,
                    8,
                    20));

        AttendanceEvent clockIn =
            CreateEvent(
                record,
                Guid.NewGuid(),
                AttendanceEventType.ClockIn,
                Utc(
                    2026,
                    8,
                    20,
                    1,
                    0));

        await SeedRecordAndEventsAsync(
            options,
            record,
            clockIn);

        AttendanceEvent clockOut =
            CreateEvent(
                record,
                Guid.NewGuid(),
                AttendanceEventType.ClockOut,
                Utc(
                    2026,
                    8,
                    20,
                    10,
                    0));

        var persistence =
            CreatePersistence(
                options);

        await persistence.AppendAsync(
            newRecord: null,
            clockOut,
            expectedLastEvent:
                clockIn);

        await using var verificationContext =
            new HrManagementDbContext(
                options);

        List<AttendanceEvent> events =
            await verificationContext
                .AttendanceEvents
                .AsNoTracking()
                .OrderBy(
                    item =>
                        item.OccurredAtUtc)
                .ToListAsync();

        Assert.Equal(
            2,
            events.Count);

        Assert.Equal(
            clockIn.Id,
            events[0].Id);

        Assert.Equal(
            clockOut.Id,
            events[1].Id);

        Assert.Equal(
            AttendanceEventType.ClockOut,
            events[1].EventType);
    }

    [Fact]
    public async Task StaleExpectedLastEvent_ThrowsConcurrencyAndDoesNotInsert()
    {
        await using SqliteConnection connection =
            await CreateOpenConnectionAsync();

        DbContextOptions<HrManagementDbContext> options =
            CreateOptions(
                connection);

        await EnsureCreatedAsync(
            options);

        SeedIds ids =
            await SeedScheduleContextAsync(
                options);

        AttendanceRecord record =
            CreateRecord(
                ids,
                Guid.NewGuid(),
                new DateOnly(
                    2026,
                    8,
                    20));

        AttendanceEvent clockIn =
            CreateEvent(
                record,
                Guid.NewGuid(),
                AttendanceEventType.ClockIn,
                Utc(
                    2026,
                    8,
                    20,
                    1,
                    0));

        AttendanceEvent clockOut =
            CreateEvent(
                record,
                Guid.NewGuid(),
                AttendanceEventType.ClockOut,
                Utc(
                    2026,
                    8,
                    20,
                    5,
                    0));

        await SeedRecordAndEventsAsync(
            options,
            record,
            clockIn,
            clockOut);

        AttendanceEvent nextClockIn =
            CreateEvent(
                record,
                Guid.NewGuid(),
                AttendanceEventType.ClockIn,
                Utc(
                    2026,
                    8,
                    20,
                    6,
                    0));

        var persistence =
            CreatePersistence(
                options);

        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(
            () =>
                persistence.AppendAsync(
                    newRecord: null,
                    nextClockIn,
                    expectedLastEvent:
                        clockIn));

        await using var verificationContext =
            new HrManagementDbContext(
                options);

        Assert.Equal(
            2,
            await verificationContext
                .AttendanceEvents
                .CountAsync());

        Assert.False(
            await verificationContext
                .AttendanceEvents
                .AnyAsync(
                    item =>
                        item.Id ==
                        nextClockIn.Id));
    }

    [Fact]
    public async Task ExpectedLastEventNull_WhenHistoryExists_ThrowsConcurrency()
    {
        await using SqliteConnection connection =
            await CreateOpenConnectionAsync();

        DbContextOptions<HrManagementDbContext> options =
            CreateOptions(
                connection);

        await EnsureCreatedAsync(
            options);

        SeedIds ids =
            await SeedScheduleContextAsync(
                options);

        AttendanceRecord record =
            CreateRecord(
                ids,
                Guid.NewGuid(),
                new DateOnly(
                    2026,
                    8,
                    20));

        AttendanceEvent clockIn =
            CreateEvent(
                record,
                Guid.NewGuid(),
                AttendanceEventType.ClockIn,
                Utc(
                    2026,
                    8,
                    20,
                    1,
                    0));

        await SeedRecordAndEventsAsync(
            options,
            record,
            clockIn);

        AttendanceEvent clockOut =
            CreateEvent(
                record,
                Guid.NewGuid(),
                AttendanceEventType.ClockOut,
                Utc(
                    2026,
                    8,
                    20,
                    5,
                    0));

        var persistence =
            CreatePersistence(
                options);

        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(
            () =>
                persistence.AppendAsync(
                    newRecord: null,
                    clockOut,
                    expectedLastEvent: null));

        await using var verificationContext =
            new HrManagementDbContext(
                options);

        Assert.Single(
            await verificationContext
                .AttendanceEvents
                .AsNoTracking()
                .ToListAsync());
    }

    [Fact]
    public async Task DuplicateEmployeeWorkDate_ThrowsConcurrency()
    {
        await using SqliteConnection connection =
            await CreateOpenConnectionAsync();

        DbContextOptions<HrManagementDbContext> options =
            CreateOptions(
                connection);

        await EnsureCreatedAsync(
            options);

        SeedIds ids =
            await SeedScheduleContextAsync(
                options);

        DateOnly workDate =
            new(
                2026,
                8,
                20);

        AttendanceRecord existingRecord =
            CreateRecord(
                ids,
                Guid.NewGuid(),
                workDate);

        await SeedRecordAndEventsAsync(
            options,
            existingRecord);

        AttendanceRecord duplicateRecord =
            CreateRecord(
                ids,
                Guid.NewGuid(),
                workDate);

        AttendanceEvent clockIn =
            CreateEvent(
                duplicateRecord,
                Guid.NewGuid(),
                AttendanceEventType.ClockIn,
                Utc(
                    2026,
                    8,
                    20,
                    1,
                    0));

        var persistence =
            CreatePersistence(
                options);

        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(
            () =>
                persistence.AppendAsync(
                    duplicateRecord,
                    clockIn,
                    expectedLastEvent: null));

        await using var verificationContext =
            new HrManagementDbContext(
                options);

        Assert.Single(
            await verificationContext
                .AttendanceRecords
                .AsNoTracking()
                .ToListAsync());

        Assert.False(
            await verificationContext
                .AttendanceRecords
                .AnyAsync(
                    item =>
                        item.Id ==
                        duplicateRecord.Id));
    }

    [Fact]
    public async Task InvalidSequence_IsRejectedAndDoesNotInsert()
    {
        await using SqliteConnection connection =
            await CreateOpenConnectionAsync();

        DbContextOptions<HrManagementDbContext> options =
            CreateOptions(
                connection);

        await EnsureCreatedAsync(
            options);

        SeedIds ids =
            await SeedScheduleContextAsync(
                options);

        AttendanceRecord record =
            CreateRecord(
                ids,
                Guid.NewGuid(),
                new DateOnly(
                    2026,
                    8,
                    20));

        AttendanceEvent firstClockIn =
            CreateEvent(
                record,
                Guid.NewGuid(),
                AttendanceEventType.ClockIn,
                Utc(
                    2026,
                    8,
                    20,
                    1,
                    0));

        await SeedRecordAndEventsAsync(
            options,
            record,
            firstClockIn);

        AttendanceEvent repeatedClockIn =
            CreateEvent(
                record,
                Guid.NewGuid(),
                AttendanceEventType.ClockIn,
                Utc(
                    2026,
                    8,
                    20,
                    2,
                    0));

        var persistence =
            CreatePersistence(
                options);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () =>
                persistence.AppendAsync(
                    newRecord: null,
                    repeatedClockIn,
                    expectedLastEvent:
                        firstClockIn));

        await using var verificationContext =
            new HrManagementDbContext(
                options);

        Assert.Single(
            await verificationContext
                .AttendanceEvents
                .AsNoTracking()
                .ToListAsync());

        Assert.False(
            await verificationContext
                .AttendanceEvents
                .AnyAsync(
                    item =>
                        item.Id ==
                        repeatedClockIn.Id));
    }

    [Fact]
    public async Task FailedEventInsert_RollsBackNewAttendanceRecord()
    {
        await using SqliteConnection connection =
            await CreateOpenConnectionAsync();

        DbContextOptions<HrManagementDbContext> options =
            CreateOptions(
                connection);

        await EnsureCreatedAsync(
            options);

        SeedIds ids =
            await SeedScheduleContextAsync(
                options);

        AttendanceRecord existingRecord =
            CreateRecord(
                ids,
                Guid.NewGuid(),
                new DateOnly(
                    2026,
                    8,
                    20));

        Guid conflictingEventId =
            Guid.NewGuid();

        AttendanceEvent existingClockIn =
            CreateEvent(
                existingRecord,
                conflictingEventId,
                AttendanceEventType.ClockIn,
                Utc(
                    2026,
                    8,
                    20,
                    1,
                    0));

        await SeedRecordAndEventsAsync(
            options,
            existingRecord,
            existingClockIn);

        AttendanceRecord newRecord =
            CreateRecord(
                ids,
                Guid.NewGuid(),
                new DateOnly(
                    2026,
                    8,
                    21));

        AttendanceEvent conflictingClockIn =
            CreateEvent(
                newRecord,
                conflictingEventId,
                AttendanceEventType.ClockIn,
                Utc(
                    2026,
                    8,
                    21,
                    1,
                    0));

        var persistence =
            CreatePersistence(
                options);

        await Assert.ThrowsAsync<DbUpdateException>(
            () =>
                persistence.AppendAsync(
                    newRecord,
                    conflictingClockIn,
                    expectedLastEvent: null));

        await using var verificationContext =
            new HrManagementDbContext(
                options);

        Assert.False(
            await verificationContext
                .AttendanceRecords
                .AnyAsync(
                    item =>
                        item.Id ==
                        newRecord.Id));

        Assert.Equal(
            1,
            await verificationContext
                .AttendanceRecords
                .CountAsync());

        Assert.Equal(
            1,
            await verificationContext
                .AttendanceEvents
                .CountAsync());

        Assert.Equal(
            conflictingEventId,
            await verificationContext
                .AttendanceEvents
                .Select(
                    item =>
                        item.Id)
                .SingleAsync());
    }

    [Fact]
    public async Task FirstPunch_WhenPeriodIsClosed_RejectsRecordAndEvent()
    {
        await using SqliteConnection connection =
            await CreateOpenConnectionAsync();

        DbContextOptions<HrManagementDbContext> options =
            CreateOptions(
                connection);

        await EnsureCreatedAsync(
            options);

        SeedIds ids =
            await SeedScheduleContextAsync(
                options);

        DateOnly workDate =
            new(
                2026,
                8,
                20);

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
            var seedContext =
                new HrManagementDbContext(
                    options))
        {
            seedContext.TimesheetPeriods.Add(
                period);

            await seedContext.SaveChangesAsync();
        }

        AttendanceRecord record =
            CreateRecord(
                ids,
                Guid.NewGuid(),
                workDate);

        AttendanceEvent clockIn =
            CreateEvent(
                record,
                Guid.NewGuid(),
                AttendanceEventType.ClockIn,
                Utc(
                    2026,
                    8,
                    20,
                    1,
                    0));

        EfAttendancePunchPersistence persistence =
            CreatePersistence(
                options);

        InvalidOperationException exception =
            await Assert.ThrowsAsync<InvalidOperationException>(
                () =>
                    persistence.AppendAsync(
                        record,
                        clockIn,
                        expectedLastEvent: null));

        Assert.Equal(
            "Kỳ công của ngày chấm công đã được đóng. Không thể thay đổi dữ liệu chấm công.",
            exception.Message);

        await using var verificationContext =
            new HrManagementDbContext(
                options);

        Assert.Empty(
            await verificationContext
                .AttendanceRecords
                .AsNoTracking()
                .ToArrayAsync());

        Assert.Empty(
            await verificationContext
                .AttendanceEvents
                .AsNoTracking()
                .ToArrayAsync());
    }

    [Fact]
    public async Task ExistingRecord_WhenPeriodIsClosed_UsesRecordWorkDateAndRejectsEvent()
    {
        await using SqliteConnection connection =
            await CreateOpenConnectionAsync();

        DbContextOptions<HrManagementDbContext> options =
            CreateOptions(
                connection);

        await EnsureCreatedAsync(
            options);

        SeedIds ids =
            await SeedScheduleContextAsync(
                options);

        DateOnly recordWorkDate =
            new(
                2026,
                8,
                31);

        AttendanceRecord record =
            CreateRecord(
                ids,
                Guid.NewGuid(),
                recordWorkDate);

        AttendanceEvent clockIn =
            CreateEvent(
                record,
                Guid.NewGuid(),
                AttendanceEventType.ClockIn,
                Utc(
                    2026,
                    8,
                    31,
                    15,
                    0));

        await SeedRecordAndEventsAsync(
            options,
            record,
            clockIn);

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
                18,
                0,
                0,
                DateTimeKind.Utc),
            "user-1",
            "admin");

        await using (
            var seedContext =
                new HrManagementDbContext(
                    options))
        {
            seedContext.TimesheetPeriods.Add(
                period);

            await seedContext.SaveChangesAsync();
        }

        AttendanceEvent clockOut =
            CreateEvent(
                record,
                Guid.NewGuid(),
                AttendanceEventType.ClockOut,
                Utc(
                    2026,
                    9,
                    1,
                    0,
                    30));

        EfAttendancePunchPersistence persistence =
            CreatePersistence(
                options);

        InvalidOperationException exception =
            await Assert.ThrowsAsync<InvalidOperationException>(
                () =>
                    persistence.AppendAsync(
                        newRecord: null,
                        clockOut,
                        expectedLastEvent:
                            clockIn));

        Assert.Equal(
            "Kỳ công của ngày chấm công đã được đóng. Không thể thay đổi dữ liệu chấm công.",
            exception.Message);

        await using var verificationContext =
            new HrManagementDbContext(
                options);

        AttendanceEvent[] events =
            await verificationContext
                .AttendanceEvents
                .AsNoTracking()
                .ToArrayAsync();

        AttendanceEvent persistedClockIn =
            Assert.Single(
                events);

        Assert.Equal(
            clockIn.Id,
            persistedClockIn.Id);

        Assert.False(
            await verificationContext
                .AttendanceEvents
                .AnyAsync(
                    item =>
                        item.Id ==
                        clockOut.Id));
    }

    private static EfAttendancePunchPersistence CreatePersistence(
        DbContextOptions<HrManagementDbContext> options)
    {
        return new EfAttendancePunchPersistence(
            new TestDbContextFactory(
                options));
    }

    private static AttendanceRecord CreateRecord(
        SeedIds ids,
        Guid recordId,
        DateOnly workDate)
    {
        return new AttendanceRecord(
            recordId,
            ids.EmployeeId,
            ids.EmploymentPeriodId,
            ids.AssignmentId,
            ids.ScheduleId,
            workDate,
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

    private static AttendanceEvent CreateEvent(
        AttendanceRecord record,
        Guid eventId,
        AttendanceEventType eventType,
        DateTime occurredAtUtc)
    {
        return new AttendanceEvent(
            eventId,
            record.Id,
            record.EmployeeId,
            eventType,
            occurredAtUtc);
    }

    private static async Task SeedRecordAndEventsAsync(
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

    private static async Task<SeedIds>
        SeedScheduleContextAsync(
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

        string employeeCode =
            $"EMP{ids.EmployeeId:N}"[..20];

        await dbContext
            .Employees
            .AddAsync(
                new Employee(
                    ids.EmployeeId,
                    employeeCode,
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

        await dbContext
            .EmploymentPeriods
            .AddAsync(
                new EmploymentPeriod(
                    ids.EmploymentPeriodId,
                    ids.EmployeeId,
                    new DateOnly(
                        2026,
                        1,
                        1)));

        await dbContext
            .WorkSchedules
            .AddAsync(
                new WorkSchedule(
                    ids.ScheduleId,
                    $"S{ids.ScheduleId:N}"[..20],
                    "Lịch kiểm thử",
                    "SE Asia Standard Time"));

        await dbContext
            .EmployeeWorkScheduleAssignments
            .AddAsync(
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
        int year,
        int month,
        int day,
        int hour,
        int minute)
    {
        return new DateTime(
            year,
            month,
            day,
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
