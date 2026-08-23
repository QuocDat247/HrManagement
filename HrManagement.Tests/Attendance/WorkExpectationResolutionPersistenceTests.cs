using HrManagement.Application.Attendance.Expectations;
using HrManagement.Domain.Attendance.Calendars;
using HrManagement.Domain.Attendance.Expectations;
using HrManagement.Domain.Attendance.Schedules;
using HrManagement.Infrastructure.Attendance.Expectations;
using HrManagement.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace HrManagement.Tests.Attendance;

public sealed class WorkExpectationResolutionPersistenceTests
{
    [Fact]
    public async Task LoadAsync_ReturnsOnlyRequestedDateSources()
    {
        DateOnly workDate =
            new(
                2026,
                9,
                2);

        await using TestDatabase database =
            await TestDatabase.CreateAsync();

        WorkSchedule firstSchedule =
            await SeedScheduleAsync(
                database);

        WorkSchedule secondSchedule =
            await SeedScheduleAsync(
                database);

        WorkSchedule unrelatedSchedule =
            await SeedScheduleAsync(
                database);

        WorkScheduleDay firstWeeklyDay =
            await SeedWorkingDayAsync(
                database,
                firstSchedule.Id,
                workDate.DayOfWeek);

        WorkScheduleDay secondWeeklyDay =
            await SeedWorkingDayAsync(
                database,
                secondSchedule.Id,
                workDate.DayOfWeek);

        await SeedWorkingDayAsync(
            database,
            unrelatedSchedule.Id,
            workDate.DayOfWeek);

        await SeedWorkingDayAsync(
            database,
            firstSchedule.Id,
            workDate.AddDays(
                1).DayOfWeek);

        HolidayCalendarDay holiday =
            await SeedHolidayAsync(
                database,
                workDate,
                isActive: true);

        WorkScheduleDateOverride requestedOverride =
            await SeedOverrideAsync(
                database,
                firstSchedule.Id,
                workDate);

        await SeedOverrideAsync(
            database,
            unrelatedSchedule.Id,
            workDate);

        await SeedOverrideAsync(
            database,
            secondSchedule.Id,
            workDate.AddDays(
                1));

        WorkExpectationResolutionData data =
            await database.Persistence
                .LoadAsync(
                    workDate,
                    new[]
                    {
                        firstSchedule.Id,
                        secondSchedule.Id
                    });

        Assert.NotNull(
            data.Holiday);

        Assert.Equal(
            holiday.Id,
            data.Holiday.Id);

        Assert.Equal(
            2,
            data.WeeklyDays.Count);

        Assert.Contains(
            data.WeeklyDays,
            day =>
                day.Id ==
                firstWeeklyDay.Id);

        Assert.Contains(
            data.WeeklyDays,
            day =>
                day.Id ==
                secondWeeklyDay.Id);

        WorkScheduleDateOverride actualOverride =
            Assert.Single(
                data.DateOverrides);

        Assert.Equal(
            requestedOverride.Id,
            actualOverride.Id);
    }

    [Fact]
    public async Task LoadAsync_ReturnsInactiveHolidayForResolverDecision()
    {
        DateOnly workDate =
            new(
                2026,
                9,
                2);

        await using TestDatabase database =
            await TestDatabase.CreateAsync();

        WorkSchedule schedule =
            await SeedScheduleAsync(
                database);

        HolidayCalendarDay holiday =
            await SeedHolidayAsync(
                database,
                workDate,
                isActive: false);

        WorkExpectationResolutionData data =
            await database.Persistence
                .LoadAsync(
                    workDate,
                    new[]
                    {
                        schedule.Id
                    });

        Assert.NotNull(
            data.Holiday);

        Assert.Equal(
            holiday.Id,
            data.Holiday.Id);

        Assert.False(
            data.Holiday.IsActive);
    }

    [Fact]
    public async Task Resolver_WithEfPersistence_AppliesOverrideBeforeHoliday()
    {
        DateOnly workDate =
            new(
                2026,
                9,
                2);

        await using TestDatabase database =
            await TestDatabase.CreateAsync();

        WorkSchedule overriddenSchedule =
            await SeedScheduleAsync(
                database);

        WorkSchedule holidaySchedule =
            await SeedScheduleAsync(
                database);

        await SeedWorkingDayAsync(
            database,
            overriddenSchedule.Id,
            workDate.DayOfWeek);

        await SeedWorkingDayAsync(
            database,
            holidaySchedule.Id,
            workDate.DayOfWeek);

        await SeedHolidayAsync(
            database,
            workDate,
            isActive: true);

        WorkScheduleDateOverride dateOverride =
            await SeedOverrideAsync(
                database,
                overriddenSchedule.Id,
                workDate);

        var resolver =
            new WorkExpectationResolver(
                database.Persistence);

        IReadOnlyDictionary<Guid, ResolvedWorkExpectation>
            result =
                await resolver.ResolveManyAsync(
                    new[]
                    {
                        overriddenSchedule.Id,
                        holidaySchedule.Id
                    },
                    workDate);

        Assert.Equal(
            2,
            result.Count);

        Assert.Equal(
            WorkExpectationSource.DateOverride,
            result[overriddenSchedule.Id].Source);

        Assert.Equal(
            dateOverride.Id,
            result[overriddenSchedule.Id].SourceId);

        Assert.True(
            result[overriddenSchedule.Id].IsWorkingDay);

        Assert.Equal(
            WorkExpectationSource.Holiday,
            result[holidaySchedule.Id].Source);

        Assert.False(
            result[holidaySchedule.Id].IsWorkingDay);
    }

    [Fact]
    public async Task Resolver_WithEfPersistence_UsesWeeklyWhenHolidayIsInactive()
    {
        DateOnly workDate =
            new(
                2026,
                9,
                3);

        await using TestDatabase database =
            await TestDatabase.CreateAsync();

        WorkSchedule schedule =
            await SeedScheduleAsync(
                database,
                isActive: false);

        WorkScheduleDay weeklyDay =
            await SeedWorkingDayAsync(
                database,
                schedule.Id,
                workDate.DayOfWeek);

        await SeedHolidayAsync(
            database,
            workDate,
            isActive: false);

        var resolver =
            new WorkExpectationResolver(
                database.Persistence);

        ResolvedWorkExpectation expectation =
            Assert.IsType<ResolvedWorkExpectation>(
                await resolver.ResolveAsync(
                    schedule.Id,
                    workDate));

        Assert.Equal(
            WorkExpectationSource.WeeklySchedule,
            expectation.Source);

        Assert.Equal(
            weeklyDay.Id,
            expectation.SourceId);

        Assert.True(
            expectation.IsWorkingDay);

        Assert.Equal(
            480,
            expectation.PlannedMinutes);
    }

    private static async Task<WorkSchedule> SeedScheduleAsync(
        TestDatabase database,
        bool isActive = true)
    {
        Guid id =
            Guid.NewGuid();

        string suffix =
            id
                .ToString("N")[..8]
                .ToUpperInvariant();

        var schedule =
            new WorkSchedule(
                id,
                $"S-{suffix}",
                $"Lịch {suffix}",
                "SE Asia Standard Time",
                isActive);

        await using HrManagementDbContext dbContext =
            await database.Factory
                .CreateDbContextAsync();

        dbContext.WorkSchedules.Add(
            schedule);

        await dbContext.SaveChangesAsync();

        return schedule;
    }

    private static async Task<WorkScheduleDay> SeedWorkingDayAsync(
        TestDatabase database,
        Guid workScheduleId,
        DayOfWeek dayOfWeek)
    {
        var day =
            new WorkScheduleDay(
                Guid.NewGuid(),
                workScheduleId,
                dayOfWeek,
                true,
                new TimeOnly(
                    8,
                    0),
                new TimeOnly(
                    17,
                    0),
                60);

        await using HrManagementDbContext dbContext =
            await database.Factory
                .CreateDbContextAsync();

        dbContext.WorkScheduleDays.Add(
            day);

        await dbContext.SaveChangesAsync();

        return day;
    }

    private static async Task<HolidayCalendarDay> SeedHolidayAsync(
        TestDatabase database,
        DateOnly workDate,
        bool isActive)
    {
        var holiday =
            new HolidayCalendarDay(
                Guid.NewGuid(),
                workDate,
                "Quốc khánh",
                isActive);

        await using HrManagementDbContext dbContext =
            await database.Factory
                .CreateDbContextAsync();

        dbContext.HolidayCalendarDays.Add(
            holiday);

        await dbContext.SaveChangesAsync();

        return holiday;
    }

    private static async Task<WorkScheduleDateOverride> SeedOverrideAsync(
        TestDatabase database,
        Guid workScheduleId,
        DateOnly workDate)
    {
        var item =
            new WorkScheduleDateOverride(
                Guid.NewGuid(),
                workScheduleId,
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

        await using HrManagementDbContext dbContext =
            await database.Factory
                .CreateDbContextAsync();

        dbContext.WorkScheduleDateOverrides.Add(
            item);

        await dbContext.SaveChangesAsync();

        return item;
    }

    private sealed class TestDatabase
        : IAsyncDisposable
    {
        private readonly SqliteConnection
            _connection;

        public TestDbContextFactory Factory
        {
            get;
        }

        public EfWorkExpectationResolutionPersistence Persistence
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
                new EfWorkExpectationResolutionPersistence(
                    factory);
        }

        public static async Task<TestDatabase> CreateAsync()
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
                await factory
                    .CreateDbContextAsync();

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

        public Task<HrManagementDbContext> CreateDbContextAsync(
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(
                CreateDbContext());
        }
    }
}
