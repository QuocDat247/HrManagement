using HrManagement.Domain.Attendance.Calculations;
using HrManagement.Domain.Attendance.Records;
using HrManagement.Domain.Attendance.Schedules;
using HrManagement.Domain.Employees;
using HrManagement.Infrastructure.Attendance.Records;
using HrManagement.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace HrManagement.Tests.Attendance;

public sealed class AttendancePersistenceTests
{
    [Fact]
    public async Task AttendanceRecord_RoundTrip_PreservesScheduleSnapshot()
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

        Guid recordId =
            Guid.NewGuid();

        await AddRecordAsync(
            options,
            new AttendanceRecord(
                recordId,
                ids.EmployeeId,
                ids.EmploymentPeriodId,
                ids.AssignmentId,
                ids.ScheduleId,
                new DateOnly(
                    2026,
                    8,
                    20),
                " SE Asia Standard Time ",
                true,
                new TimeOnly(
                    8,
                    0),
                new TimeOnly(
                    17,
                    0),
                60));

        await using var dbContext =
            new HrManagementDbContext(
                options);

        AttendanceRecord saved =
            await dbContext
                .AttendanceRecords
                .AsNoTracking()
                .SingleAsync();

        Assert.Equal(
            recordId,
            saved.Id);

        Assert.Equal(
            new DateOnly(
                2026,
                8,
                20),
            saved.WorkDate);

        Assert.Equal(
            "SE Asia Standard Time",
            saved.TimeZoneId);

        Assert.True(
            saved.IsWorkingDay);

        Assert.Equal(
            new TimeOnly(
                8,
                0),
            saved.ExpectedStartTime);

        Assert.Equal(
            new TimeOnly(
                17,
                0),
            saved.ExpectedEndTime);

        Assert.Equal(
            60,
            saved.ExpectedBreakMinutes);

        Assert.Equal(
            480,
            saved.ExpectedPlannedMinutes);
    }

    [Fact]
    public async Task OvernightRecord_RoundTrip_PreservesBusinessWorkDate()
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

        DateOnly businessWorkDate =
            new(
                2026,
                8,
                20);

        await AddRecordAsync(
            options,
            new AttendanceRecord(
                Guid.NewGuid(),
                ids.EmployeeId,
                ids.EmploymentPeriodId,
                ids.AssignmentId,
                ids.ScheduleId,
                businessWorkDate,
                "SE Asia Standard Time",
                true,
                new TimeOnly(
                    22,
                    0),
                new TimeOnly(
                    6,
                    0),
                60));

        await using var dbContext =
            new HrManagementDbContext(
                options);

        AttendanceRecord saved =
            await dbContext
                .AttendanceRecords
                .AsNoTracking()
                .SingleAsync();

        Assert.Equal(
            businessWorkDate,
            saved.WorkDate);

        Assert.True(
            saved.IsOvernight);

        Assert.Equal(
            420,
            saved.ExpectedPlannedMinutes);
    }

    [Fact]
    public async Task AttendanceEvent_RoundTrip_PreservesUtcTimestamp()
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

        Guid recordId =
            Guid.NewGuid();

        await AddRecordAsync(
            options,
            CreateDayRecord(
                ids,
                recordId));

        DateTime occurredAtUtc =
            new(
                2026,
                8,
                20,
                1,
                15,
                37,
                DateTimeKind.Utc);

        await using (
            var dbContext =
                new HrManagementDbContext(
                    options))
        {
            await dbContext
                .AttendanceEvents
                .AddAsync(
                    new AttendanceEvent(
                        Guid.NewGuid(),
                        recordId,
                        ids.EmployeeId,
                        AttendanceEventType.ClockIn,
                        occurredAtUtc));

            await dbContext.SaveChangesAsync();
        }

        await using var verificationContext =
            new HrManagementDbContext(
                options);

        AttendanceEvent saved =
            await verificationContext
                .AttendanceEvents
                .AsNoTracking()
                .SingleAsync();

        Assert.Equal(
            occurredAtUtc,
            saved.OccurredAtUtc);

        Assert.Equal(
            occurredAtUtc.Ticks,
            saved.OccurredAtUtc.Ticks);

        Assert.Equal(
            DateTimeKind.Utc,
            saved.OccurredAtUtc.Kind);
    }

    [Fact]
    public async Task EventRepository_ReturnsEventsInChronologicalOrder()
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

        Guid recordId =
            Guid.NewGuid();

        await AddRecordAsync(
            options,
            CreateDayRecord(
                ids,
                recordId));

        Guid firstId =
            Guid.NewGuid();

        Guid secondId =
            Guid.NewGuid();

        Guid thirdId =
            Guid.NewGuid();

        await using (
            var dbContext =
                new HrManagementDbContext(
                    options))
        {
            await dbContext
                .AttendanceEvents
                .AddRangeAsync(
                    new AttendanceEvent(
                        thirdId,
                        recordId,
                        ids.EmployeeId,
                        AttendanceEventType.ClockIn,
                        Utc(
                            13,
                            0)),
                    new AttendanceEvent(
                        firstId,
                        recordId,
                        ids.EmployeeId,
                        AttendanceEventType.ClockIn,
                        Utc(
                            8,
                            0)),
                    new AttendanceEvent(
                        secondId,
                        recordId,
                        ids.EmployeeId,
                        AttendanceEventType.ClockOut,
                        Utc(
                            12,
                            0)));

            await dbContext.SaveChangesAsync();
        }

        var repository =
            new EfAttendanceEventRepository(
                new TestDbContextFactory(
                    options));

        IReadOnlyList<AttendanceEvent> events =
            await repository
                .GetByAttendanceRecordIdAsync(
                    recordId);

        Assert.Equal(
            3,
            events.Count);

        Assert.Equal(
            firstId,
            events[0].Id);

        Assert.Equal(
            secondId,
            events[1].Id);

        Assert.Equal(
            thirdId,
            events[2].Id);
    }

    [Fact]
    public async Task RecordRepository_GetsByEmployeeAndWorkDate()
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

        Guid recordId =
            Guid.NewGuid();

        DateOnly workDate =
            new(
                2026,
                8,
                20);

        await AddRecordAsync(
            options,
            CreateDayRecord(
                ids,
                recordId,
                workDate));

        var repository =
            new EfAttendanceRecordRepository(
                new TestDbContextFactory(
                    options));

        AttendanceRecord? result =
            await repository
                .GetByEmployeeAndWorkDateAsync(
                    ids.EmployeeId,
                    workDate);

        Assert.NotNull(
            result);

        Assert.Equal(
            recordId,
            result!.Id);
    }

    [Fact]
    public async Task DuplicateEmployeeWorkDate_IsRejected()
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

        await AddRecordAsync(
            options,
            CreateDayRecord(
                ids,
                Guid.NewGuid(),
                workDate));

        await using var dbContext =
            new HrManagementDbContext(
                options);

        await dbContext
            .AttendanceRecords
            .AddAsync(
                CreateDayRecord(
                    ids,
                    Guid.NewGuid(),
                    workDate));

        await Assert.ThrowsAsync<DbUpdateException>(
            () =>
                dbContext.SaveChangesAsync());
    }

    [Fact]
    public async Task DeletingRecordWithRawEvents_IsRestricted()
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

        Guid recordId =
            Guid.NewGuid();

        await AddRecordAsync(
            options,
            CreateDayRecord(
                ids,
                recordId));

        await using (
            var seedContext =
                new HrManagementDbContext(
                    options))
        {
            await seedContext
                .AttendanceEvents
                .AddAsync(
                    new AttendanceEvent(
                        Guid.NewGuid(),
                        recordId,
                        ids.EmployeeId,
                        AttendanceEventType.ClockIn,
                        Utc(
                            8,
                            0)));

            await seedContext.SaveChangesAsync();
        }

        await using var deleteContext =
            new HrManagementDbContext(
                options);

        AttendanceRecord record =
            await deleteContext
                .AttendanceRecords
                .SingleAsync(
                    item =>
                        item.Id ==
                        recordId);

        deleteContext
            .AttendanceRecords
            .Remove(
                record);

        await Assert.ThrowsAsync<DbUpdateException>(
            () =>
                deleteContext.SaveChangesAsync());
    }

    [Fact]
    public async Task AttendanceRecord_RoundTrip_PreservesCalculationState()
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
            CreateDayRecord(
                ids,
                Guid.NewGuid());

        IReadOnlyList<AttendanceEvent> events =
        [
            new AttendanceEvent(
            Guid.NewGuid(),
            record.Id,
            record.EmployeeId,
            AttendanceEventType.ClockIn,
            new DateTime(
                2026,
                8,
                20,
                8,
                10,
                0,
                DateTimeKind.Utc)),

        new AttendanceEvent(
            Guid.NewGuid(),
            record.Id,
            record.EmployeeId,
            AttendanceEventType.ClockOut,
            new DateTime(
                2026,
                8,
                20,
                16,
                45,
                0,
                DateTimeKind.Utc))
        ];

        DailyAttendanceCalculation daily =
            DailyAttendanceCalculator.Calculate(
                record,
                events);

        AttendanceScheduleAdherence adherence =
            AttendanceScheduleAdherenceCalculator.Calculate(
                record,
                daily,
                new AttendanceScheduleWindow(
                    new DateTime(
                        2026,
                        8,
                        20,
                        8,
                        0,
                        0,
                        DateTimeKind.Utc),
                    new DateTime(
                        2026,
                        8,
                        20,
                        17,
                        0,
                        0,
                        DateTimeKind.Utc)),
                new AttendanceAdherencePolicy());

        record.ApplyCalculation(
            daily,
            adherence);

        await using (
            var dbContext =
                new HrManagementDbContext(
                    options))
        {
            await dbContext
                .AttendanceRecords
                .AddAsync(
                    record);

            await dbContext.SaveChangesAsync();
        }

        await using var verificationContext =
            new HrManagementDbContext(
                options);

        AttendanceRecord saved =
            await verificationContext
                .AttendanceRecords
                .AsNoTracking()
                .SingleAsync(
                    item =>
                        item.Id ==
                        record.Id);

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
    public async Task AttendanceRecord_RoundTrip_DefaultsToNotCalculated()
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
            CreateDayRecord(
                ids,
                Guid.NewGuid());

        await using (
            var dbContext =
                new HrManagementDbContext(
                    options))
        {
            await dbContext
                .AttendanceRecords
                .AddAsync(
                    record);

            await dbContext.SaveChangesAsync();
        }

        await using var verificationContext =
            new HrManagementDbContext(
                options);

        AttendanceRecord saved =
            await verificationContext
                .AttendanceRecords
                .AsNoTracking()
                .SingleAsync(
                    item =>
                        item.Id ==
                        record.Id);

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
    }

    private static AttendanceRecord CreateDayRecord(
        SeedIds ids,
        Guid recordId,
        DateOnly? workDate = null)
    {
        return new AttendanceRecord(
            recordId,
            ids.EmployeeId,
            ids.EmploymentPeriodId,
            ids.AssignmentId,
            ids.ScheduleId,
            workDate ??
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

        await dbContext
            .Employees
            .AddAsync(
                new Employee(
                    ids.EmployeeId,
                    $"EMP-{ids.EmployeeId:N}"[..20],
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
                    "OFFICE",
                    "Giờ hành chính",
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

    private static async Task AddRecordAsync(
        DbContextOptions<HrManagementDbContext> options,
        AttendanceRecord record)
    {
        await using var dbContext =
            new HrManagementDbContext(
                options);

        await dbContext
            .AttendanceRecords
            .AddAsync(
                record);

        await dbContext.SaveChangesAsync();
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
