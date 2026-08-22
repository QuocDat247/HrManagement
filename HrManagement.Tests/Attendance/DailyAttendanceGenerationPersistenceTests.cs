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
    public async Task GetCandidatesAsync_ReturnsEffectiveScheduleSnapshot()
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

        Assert.True(
            candidate.IsWorkingDay);

        Assert.Equal(
            new TimeOnly(
                8,
                0),
            candidate.ExpectedStartTime);

        Assert.Equal(
            new TimeOnly(
                17,
                0),
            candidate.ExpectedEndTime);

        Assert.Equal(
            60,
            candidate.ExpectedBreakMinutes);
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

        var service =
            new DailyAttendanceGenerationService(
                database.Persistence);

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
    public async Task GetCandidatesAsync_ReturnsNonWorkingDayCandidate()
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
                isWorkingDay:
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

        Assert.False(
            candidate.IsWorkingDay);

        Assert.Null(
            candidate.ExpectedStartTime);

        Assert.Null(
            candidate.ExpectedEndTime);

        Assert.Equal(
            0,
            candidate.ExpectedBreakMinutes);
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
            bool isWorkingDay = true)
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

        WorkScheduleDay scheduleDay =
            isWorkingDay
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

        dbContext.WorkScheduleDays.Add(
            scheduleDay);

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
