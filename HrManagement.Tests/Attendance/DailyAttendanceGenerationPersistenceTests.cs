using HrManagement.Application.Attendance.Expectations;
using HrManagement.Domain.Attendance.Calendars;
using HrManagement.Domain.Attendance.Timesheets;
using HrManagement.Domain.Attendance.Expectations;
using HrManagement.Infrastructure.Attendance.Expectations;
using HrManagement.Application.Attendance.Generation;
using HrManagement.Domain.Attendance.Records;
using HrManagement.Domain.Attendance.Schedules;
using HrManagement.Domain.Employees;
using HrManagement.Infrastructure.Attendance.Generation;
using HrManagement.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace HrManagement.Tests.Attendance;

public sealed class DailyAttendanceGenerationPersistenceTests
{
    [Fact]
    public async Task GetCandidatesAsync_ReturnsEffectiveAssignmentContext()
    {
        DateOnly workDate =
            new(
                2026,
                8,
                21);

        await using TestDatabase database =
            await TestDatabase.CreateAsync();

        SeedResult seed =
            await SeedAssignmentAsync(
                database,
                workDate,
                scheduleIsActive: false);

        IReadOnlyList<DailyAttendanceGenerationCandidate>
            candidates =
                await database.Persistence
                    .GetCandidatesAsync(
                        workDate);

        DailyAttendanceGenerationCandidate candidate =
            Assert.Single(
                candidates);

        Assert.Equal(
            seed.EmployeeId,
            candidate.EmployeeId);

        Assert.Equal(
            seed.EmploymentPeriodId,
            candidate.EmploymentPeriodId);

        Assert.Equal(
            seed.AssignmentId,
            candidate.WorkScheduleAssignmentId);

        Assert.Equal(
            seed.ScheduleId,
            candidate.WorkScheduleId);

        Assert.Equal(
            "SE Asia Standard Time",
            candidate.TimeZoneId);
    }

    [Fact]
    public async Task GetCandidatesAsync_RespectsAssignmentAndEmploymentTimelines()
    {
        DateOnly workDate =
            new(
                2026,
                8,
                21);

        await using TestDatabase database =
            await TestDatabase.CreateAsync();

        await SeedAssignmentAsync(
            database,
            workDate,
            assignmentEffectiveTo:
                workDate.AddDays(
                    -1));

        await SeedAssignmentAsync(
            database,
            workDate,
            employmentEndDate:
                workDate.AddDays(
                    -1));

        IReadOnlyList<DailyAttendanceGenerationCandidate>
            candidates =
                await database.Persistence
                    .GetCandidatesAsync(
                        workDate);

        Assert.Empty(
            candidates);
    }

    [Fact]
    public async Task GetExistingEmployeeIdsAsync_ReturnsOnlyMatchingDateAndEmployees()
    {
        DateOnly workDate =
            new(
                2026,
                8,
                21);

        await using TestDatabase database =
            await TestDatabase.CreateAsync();

        SeedResult seed =
            await SeedAssignmentAsync(
                database,
                workDate);

        var record =
            new AttendanceRecord(
                Guid.NewGuid(),
                seed.EmployeeId,
                seed.EmploymentPeriodId,
                seed.AssignmentId,
                seed.ScheduleId,
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

        await database.Persistence
            .AddRangeAsync(
                [
                    record
                ]);

        Guid unrelatedEmployeeId =
            Guid.NewGuid();

        IReadOnlyList<Guid> existing =
            await database.Persistence
                .GetExistingEmployeeIdsAsync(
                    workDate,
                    [
                        seed.EmployeeId,
                        unrelatedEmployeeId
                    ]);

        Guid actual =
            Assert.Single(
                existing);

        Assert.Equal(
            seed.EmployeeId,
            actual);
    }

    [Fact]
    public async Task GenerateAsync_RunTwice_IsIdempotent()
    {
        DateOnly workDate =
            new(
                2026,
                8,
                21);

        await using TestDatabase database =
            await TestDatabase.CreateAsync();

        SeedResult seed =
            await SeedAssignmentAsync(
                database,
                workDate);

        var expectationResolver =
            new WorkExpectationResolver(
                new EfWorkExpectationResolutionPersistence(
                    database.Factory));

        var service =
            new DailyAttendanceGenerationService(
                database.Persistence,
                expectationResolver,
                new StubAttendancePeriodLockPolicy());

        GenerateDailyAttendanceResult first =
            await service.GenerateAsync(
                new GenerateDailyAttendanceRequest(
                    workDate,
                    seed.EmployeeId));

        GenerateDailyAttendanceResult second =
            await service.GenerateAsync(
                new GenerateDailyAttendanceRequest(
                    workDate,
                    seed.EmployeeId));

        Assert.True(
            first.IsSuccessful);

        Assert.Equal(
            1,
            first.CreatedCount);

        Assert.Equal(
            0,
            first.SkippedExistingCount);

        Assert.True(
            second.IsSuccessful);

        Assert.Equal(
            0,
            second.CreatedCount);

        Assert.Equal(
            1,
            second.SkippedExistingCount);

        await using HrManagementDbContext dbContext =
            await database.Factory
                .CreateDbContextAsync();

        AttendanceRecord record =
            Assert.Single(
                await dbContext.AttendanceRecords
                    .AsNoTracking()
                    .ToArrayAsync());

        Assert.Equal(
            seed.EmployeeId,
            record.EmployeeId);

        Assert.Equal(
            workDate,
            record.WorkDate);
    }

    [Fact]
    public async Task GetCandidatesAsync_IncludesExactTimelineBoundaries()
    {
        DateOnly workDate =
            new(
                2026,
                8,
                21);

        await using TestDatabase database =
            await TestDatabase.CreateAsync();

        SeedResult seed =
            await SeedAssignmentAsync(
                database,
                workDate,
                assignmentEffectiveFrom:
                    workDate,
                assignmentEffectiveTo:
                    workDate,
                employmentStartDate:
                    workDate,
                employmentEndDate:
                    workDate);

        IReadOnlyList<DailyAttendanceGenerationCandidate>
            candidates =
                await database.Persistence
                    .GetCandidatesAsync(
                        workDate);

        DailyAttendanceGenerationCandidate candidate =
            Assert.Single(
                candidates);

        Assert.Equal(
            seed.EmployeeId,
            candidate.EmployeeId);

        Assert.Equal(
            seed.AssignmentId,
            candidate.WorkScheduleAssignmentId);
    }

    [Fact]
    public async Task GetCandidatesAsync_DoesNotRequireWeeklyDayDefinition()
    {
        DateOnly workDate =
            new(
                2026,
                8,
                23);

        await using TestDatabase database =
            await TestDatabase.CreateAsync();

        SeedResult seed =
            await SeedAssignmentAsync(
                database,
                workDate,
                createScheduleDay:
                    false);

        IReadOnlyList<DailyAttendanceGenerationCandidate>
            candidates =
                await database.Persistence
                    .GetCandidatesAsync(
                        workDate,
                        seed.EmployeeId);

        DailyAttendanceGenerationCandidate candidate =
            Assert.Single(
                candidates);

        Assert.Equal(
            seed.EmployeeId,
            candidate.EmployeeId);

        Assert.Equal(
            seed.ScheduleId,
            candidate.WorkScheduleId);
    }

    [Fact]
    public async Task GenerateAsync_WithHoliday_PersistsHolidaySnapshot()
    {
        DateOnly workDate =
            new(
                2026,
                9,
                2);

        await using TestDatabase database =
            await TestDatabase.CreateAsync();

        SeedResult seed =
            await SeedAssignmentAsync(
                database,
                workDate);

        var holiday =
            new HolidayCalendarDay(
                Guid.NewGuid(),
                workDate,
                "Quốc khánh");

        await using (
            HrManagementDbContext dbContext =
                await database.Factory.CreateDbContextAsync())
        {
            dbContext.HolidayCalendarDays.Add(
                holiday);

            await dbContext.SaveChangesAsync();
        }

        var resolver =
            new WorkExpectationResolver(
                new EfWorkExpectationResolutionPersistence(
                    database.Factory));

        var service =
            new DailyAttendanceGenerationService(
                database.Persistence,
                resolver,
                new StubAttendancePeriodLockPolicy());

        GenerateDailyAttendanceResult result =
            await service.GenerateAsync(
                new GenerateDailyAttendanceRequest(
                    workDate,
                    seed.EmployeeId));

        Assert.True(
            result.IsSuccessful);

        await using HrManagementDbContext verificationContext =
            await database.Factory.CreateDbContextAsync();

        AttendanceRecord record =
            Assert.Single(
                await verificationContext
                    .AttendanceRecords
                    .AsNoTracking()
                    .ToArrayAsync());

        Assert.False(
            record.IsWorkingDay);

        Assert.Equal(
            WorkExpectationSource.Holiday,
            record.ExpectationSource);

        Assert.Equal(
            holiday.Id,
            record.ExpectationSourceId);

        Assert.Equal(
            "Quốc khánh",
            record.ExpectationSourceName);
    }

    [Fact]
    public async Task GenerateAsync_WithDateOverride_PersistsOverrideBeforeHoliday()
    {
        DateOnly workDate =
            new(
                2026,
                9,
                2);

        await using TestDatabase database =
            await TestDatabase.CreateAsync();

        SeedResult seed =
            await SeedAssignmentAsync(
                database,
                workDate);

        var holiday =
            new HolidayCalendarDay(
                Guid.NewGuid(),
                workDate,
                "Quốc khánh");

        var dateOverride =
            new WorkScheduleDateOverride(
                Guid.NewGuid(),
                seed.ScheduleId,
                workDate,
                true,
                new TimeOnly(
                    22,
                    0),
                new TimeOnly(
                    6,
                    0),
                30,
                "Trực ngày lễ");

        await using (
            HrManagementDbContext dbContext =
                await database.Factory.CreateDbContextAsync())
        {
            dbContext.HolidayCalendarDays.Add(
                holiday);

            dbContext.WorkScheduleDateOverrides.Add(
                dateOverride);

            await dbContext.SaveChangesAsync();
        }

        var resolver =
            new WorkExpectationResolver(
                new EfWorkExpectationResolutionPersistence(
                    database.Factory));

        var service =
            new DailyAttendanceGenerationService(
                database.Persistence,
                resolver,
                new StubAttendancePeriodLockPolicy());

        GenerateDailyAttendanceResult result =
            await service.GenerateAsync(
                new GenerateDailyAttendanceRequest(
                    workDate,
                    seed.EmployeeId));

        Assert.True(
            result.IsSuccessful);

        await using HrManagementDbContext verificationContext =
            await database.Factory.CreateDbContextAsync();

        AttendanceRecord record =
            Assert.Single(
                await verificationContext
                    .AttendanceRecords
                    .AsNoTracking()
                    .ToArrayAsync());

        Assert.True(
            record.IsWorkingDay);

        Assert.True(
            record.IsOvernight);

        Assert.Equal(
            450,
            record.ExpectedPlannedMinutes);

        Assert.Equal(
            WorkExpectationSource.DateOverride,
            record.ExpectationSource);

        Assert.Equal(
            dateOverride.Id,
            record.ExpectationSourceId);

        Assert.Equal(
            "Trực ngày lễ",
            record.ExpectationSourceName);
    }

    [Fact]
    public async Task GenerateAsync_AfterExpectationConfigurationChanges_PreservesExistingSnapshot()
    {
        DateOnly workDate =
            new(
                2026,
                9,
                2);

        await using TestDatabase database =
            await TestDatabase.CreateAsync();

        SeedResult seed =
            await SeedAssignmentAsync(
                database,
                workDate);

        var holiday =
            new HolidayCalendarDay(
                Guid.NewGuid(),
                workDate,
                "Quốc khánh");

        await using (
            HrManagementDbContext dbContext =
                await database.Factory.CreateDbContextAsync())
        {
            dbContext.HolidayCalendarDays.Add(
                holiday);

            await dbContext.SaveChangesAsync();
        }

        var resolver =
            new WorkExpectationResolver(
                new EfWorkExpectationResolutionPersistence(
                    database.Factory));

        var service =
            new DailyAttendanceGenerationService(
                database.Persistence,
                resolver,
                new StubAttendancePeriodLockPolicy());

        GenerateDailyAttendanceResult first =
            await service.GenerateAsync(
                new GenerateDailyAttendanceRequest(
                    workDate,
                    seed.EmployeeId));

        Assert.True(
            first.IsSuccessful);

        Assert.Equal(
            1,
            first.CreatedCount);

        await using (
            HrManagementDbContext dbContext =
                await database.Factory.CreateDbContextAsync())
        {
            HolidayCalendarDay persistedHoliday =
                await dbContext.HolidayCalendarDays
                    .SingleAsync(
                        item =>
                            item.Id ==
                            holiday.Id);

            persistedHoliday.Deactivate();

            var dateOverride =
                new WorkScheduleDateOverride(
                    Guid.NewGuid(),
                    seed.ScheduleId,
                    workDate,
                    true,
                    new TimeOnly(
                        22,
                        0),
                    new TimeOnly(
                        6,
                        0),
                    30,
                    "Trực thay thế");

            dbContext.WorkScheduleDateOverrides.Add(
                dateOverride);

            await dbContext.SaveChangesAsync();
        }

        GenerateDailyAttendanceResult second =
            await service.GenerateAsync(
                new GenerateDailyAttendanceRequest(
                    workDate,
                    seed.EmployeeId));

        Assert.True(
            second.IsSuccessful);

        Assert.Equal(
            0,
            second.CreatedCount);

        Assert.Equal(
            1,
            second.SkippedExistingCount);

        await using HrManagementDbContext verificationContext =
            await database.Factory.CreateDbContextAsync();

        AttendanceRecord record =
            Assert.Single(
                await verificationContext
                    .AttendanceRecords
                    .AsNoTracking()
                    .ToArrayAsync());

        Assert.False(
            record.IsWorkingDay);

        Assert.Null(
            record.ExpectedStartTime);

        Assert.Null(
            record.ExpectedEndTime);

        Assert.Equal(
            0,
            record.ExpectedPlannedMinutes);

        Assert.Equal(
            WorkExpectationSource.Holiday,
            record.ExpectationSource);

        Assert.Equal(
            holiday.Id,
            record.ExpectationSourceId);

        Assert.Equal(
            "Quốc khánh",
            record.ExpectationSourceName);
    }

    [Fact]
    public async Task AddRangeAsync_WhenPeriodIsClosed_RejectsWithoutWritingAttendance()
    {
        DateOnly workDate =
            new(
                2026,
                8,
                21);

        await using TestDatabase database =
            await TestDatabase.CreateAsync();

        SeedResult seed =
            await SeedAssignmentAsync(
                database,
                workDate);

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
            HrManagementDbContext dbContext =
                await database.Factory
                    .CreateDbContextAsync())
        {
            dbContext.TimesheetPeriods.Add(
                period);

            await dbContext.SaveChangesAsync();
        }

        var record =
            new AttendanceRecord(
                Guid.NewGuid(),
                seed.EmployeeId,
                seed.EmploymentPeriodId,
                seed.AssignmentId,
                seed.ScheduleId,
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

        InvalidOperationException exception =
            await Assert.ThrowsAsync<InvalidOperationException>(
                () =>
                    database.Persistence.AddRangeAsync(
                        [record]));

        Assert.Equal(
            "Kỳ công của ngày chấm công đã được đóng. Không thể thay đổi dữ liệu chấm công.",
            exception.Message);

        await using HrManagementDbContext verificationContext =
            await database.Factory
                .CreateDbContextAsync();

        Assert.Empty(
            await verificationContext
                .AttendanceRecords
                .AsNoTracking()
                .ToArrayAsync());
    }

    private static async Task<SeedResult>
        SeedAssignmentAsync(
            TestDatabase database,
            DateOnly workDate,
            bool scheduleIsActive = true,
            DateOnly? assignmentEffectiveFrom = null,
            DateOnly? assignmentEffectiveTo = null,
            DateOnly? employmentStartDate = null,
            DateOnly? employmentEndDate = null,
            bool isWorkingDay = true,
            bool createScheduleDay = true)
    {
        Guid employeeId =
            Guid.NewGuid();

        Guid employmentPeriodId =
            Guid.NewGuid();

        Guid scheduleId =
            Guid.NewGuid();

        Guid assignmentId =
            Guid.NewGuid();

        string suffix =
            employeeId
                .ToString("N")[..8]
                .ToUpperInvariant();

        var employee =
            new Employee(
                employeeId,
                $"EMP-{suffix}",
                $"Nhân viên {suffix}",
                null,
                null,
                null,
                workDate.AddDays(
                    -30),
                "Phòng thử nghiệm",
                "Nhân viên",
                EmployeeStatus.Active);

        var employmentPeriod =
            new EmploymentPeriod(
                employmentPeriodId,
                employeeId,
                employmentStartDate
                    ?? workDate.AddDays(
                        -30),
                employmentEndDate);

        var schedule =
            new WorkSchedule(
                scheduleId,
                $"S-{suffix}",
                $"Lịch {suffix}",
                "SE Asia Standard Time",
                scheduleIsActive);

        WorkScheduleDay? scheduleDay =
            !createScheduleDay
                ? null
                : isWorkingDay
                    ? new WorkScheduleDay(
                        Guid.NewGuid(),
                        scheduleId,
                        workDate.DayOfWeek,
                        true,
                        new TimeOnly(
                            8,
                            0),
                        new TimeOnly(
                            17,
                            0),
                        60)
                    : new WorkScheduleDay(
                        Guid.NewGuid(),
                        scheduleId,
                        workDate.DayOfWeek,
                        false);

        var assignment =
            new EmployeeWorkScheduleAssignment(
                assignmentId,
                employeeId,
                employmentPeriodId,
                scheduleId,
                assignmentEffectiveFrom
                    ?? workDate.AddDays(
                        -10),
                assignmentEffectiveTo);

        await using HrManagementDbContext dbContext =
            await database.Factory
                .CreateDbContextAsync();

        dbContext.Employees.Add(
            employee);

        dbContext.EmploymentPeriods.Add(
            employmentPeriod);

        dbContext.WorkSchedules.Add(
            schedule);

        if (scheduleDay is not null)
        {
            dbContext.WorkScheduleDays.Add(
                scheduleDay);
        }

        dbContext.EmployeeWorkScheduleAssignments.Add(
            assignment);

        await dbContext.SaveChangesAsync();

        return new SeedResult(
            employeeId,
            employmentPeriodId,
            assignmentId,
            scheduleId);
    }

    private sealed record SeedResult(
        Guid EmployeeId,
        Guid EmploymentPeriodId,
        Guid AssignmentId,
        Guid ScheduleId);

    private sealed class TestDatabase
        : IAsyncDisposable
    {
        private readonly SqliteConnection
            _connection;

        public TestDbContextFactory Factory
        {
            get;
        }

        public EfDailyAttendanceGenerationPersistence Persistence
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
                new EfDailyAttendanceGenerationPersistence(
                    factory);
        }

        public static async Task<TestDatabase>
            CreateAsync()
        {
            var connection =
                new SqliteConnection(
                    "Data Source=:memory:");

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
}
