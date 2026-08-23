using HrManagement.Application.Workspaces.HolidayExceptions;
using HrManagement.Domain.Attendance.Calendars;
using HrManagement.Domain.Attendance.Schedules;
using HrManagement.Infrastructure.Persistence;
using HrManagement.Infrastructure.Workspaces.HolidayExceptions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace HrManagement.Tests.Attendance;

public sealed class HolidayExceptionWorkspaceQueryServiceTests
{
    [Fact]
    public async Task GetAsync_ReturnsYearAndSelectedScheduleSnapshot()
    {
        await using TestDatabase database =
            await TestDatabase.CreateAsync();

        WorkSchedule selectedSchedule =
            await SeedScheduleAsync(
                database,
                "OFFICE",
                true);

        WorkSchedule inactiveSchedule =
            await SeedScheduleAsync(
                database,
                "OLD",
                false);

        HolidayCalendarDay activeHoliday =
            await SeedHolidayAsync(
                database,
                new DateOnly(
                    2026,
                    9,
                    2),
                "Quốc khánh",
                true);

        HolidayCalendarDay inactiveHoliday =
            await SeedHolidayAsync(
                database,
                new DateOnly(
                    2026,
                    12,
                    31),
                "Ngày đặc biệt cũ",
                false);

        WorkScheduleDateOverride selectedOverride =
            await SeedOverrideAsync(
                database,
                selectedSchedule.Id,
                new DateOnly(
                    2026,
                    9,
                    2));

        await SeedOverrideAsync(
            database,
            inactiveSchedule.Id,
            new DateOnly(
                2026,
                9,
                2));

        HolidayExceptionWorkspaceSnapshot snapshot =
            await database.Service
                .GetAsync(
                    new HolidayExceptionWorkspaceQuery(
                        2026,
                        selectedSchedule.Id));

        Assert.Equal(
            2026,
            snapshot.Year);

        Assert.Equal(
            selectedSchedule.Id,
            snapshot.SelectedWorkScheduleId);

        Assert.Equal(
            2,
            snapshot.Schedules.Count);

        Assert.Contains(
            snapshot.Schedules,
            item =>
                item.Id ==
                    selectedSchedule.Id
                && item.IsActive);

        Assert.Contains(
            snapshot.Schedules,
            item =>
                item.Id ==
                    inactiveSchedule.Id
                && !item.IsActive);

        Assert.Equal(
            2,
            snapshot.Holidays.Count);

        Assert.Contains(
            snapshot.Holidays,
            item =>
                item.Id ==
                    activeHoliday.Id
                && item.IsActive);

        Assert.Contains(
            snapshot.Holidays,
            item =>
                item.Id ==
                    inactiveHoliday.Id
                && !item.IsActive);

        HolidayExceptionWorkspaceOverrideItem actualOverride =
            Assert.Single(
                snapshot.Overrides);

        Assert.Equal(
            selectedOverride.Id,
            actualOverride.Id);

        Assert.True(
            actualOverride.IsWorkingDay);

        Assert.True(
            actualOverride.IsOvernight);

        Assert.Equal(
            450,
            actualOverride.PlannedMinutes);

        Assert.Equal(
            "Trực ngày lễ",
            actualOverride.Note);
    }

    [Fact]
    public async Task GetAsync_FiltersHolidaysAndOverridesByYear()
    {
        await using TestDatabase database =
            await TestDatabase.CreateAsync();

        WorkSchedule schedule =
            await SeedScheduleAsync(
                database,
                "OFFICE",
                true);

        await SeedHolidayAsync(
            database,
            new DateOnly(
                2026,
                9,
                2),
            "Năm 2026",
            true);

        await SeedHolidayAsync(
            database,
            new DateOnly(
                2027,
                1,
                1),
            "Năm 2027",
            true);

        await SeedOverrideAsync(
            database,
            schedule.Id,
            new DateOnly(
                2026,
                9,
                2));

        await SeedOverrideAsync(
            database,
            schedule.Id,
            new DateOnly(
                2027,
                1,
                2));

        HolidayExceptionWorkspaceSnapshot snapshot =
            await database.Service
                .GetAsync(
                    new HolidayExceptionWorkspaceQuery(
                        2026,
                        schedule.Id));

        HolidayExceptionWorkspaceHolidayItem holiday =
            Assert.Single(
                snapshot.Holidays);

        Assert.Equal(
            2026,
            holiday.Date.Year);

        HolidayExceptionWorkspaceOverrideItem dateOverride =
            Assert.Single(
                snapshot.Overrides);

        Assert.Equal(
            2026,
            dateOverride.WorkDate.Year);
    }

    [Fact]
    public async Task GetAsync_WithoutSelectedSchedule_ReturnsNoOverrides()
    {
        await using TestDatabase database =
            await TestDatabase.CreateAsync();

        WorkSchedule schedule =
            await SeedScheduleAsync(
                database,
                "OFFICE",
                true);

        await SeedOverrideAsync(
            database,
            schedule.Id,
            new DateOnly(
                2026,
                9,
                2));

        HolidayExceptionWorkspaceSnapshot snapshot =
            await database.Service
                .GetAsync(
                    new HolidayExceptionWorkspaceQuery(
                        2026));

        Assert.Single(
            snapshot.Schedules);

        Assert.Empty(
            snapshot.Overrides);
    }

    [Fact]
    public async Task GetAsync_WithInvalidInput_Throws()
    {
        await using TestDatabase database =
            await TestDatabase.CreateAsync();

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () =>
                database.Service.GetAsync(
                    new HolidayExceptionWorkspaceQuery(
                        0)));

        await Assert.ThrowsAsync<ArgumentException>(
            () =>
                database.Service.GetAsync(
                    new HolidayExceptionWorkspaceQuery(
                        2026,
                        Guid.Empty)));
    }

    private static async Task<WorkSchedule> SeedScheduleAsync(
        TestDatabase database,
        string code,
        bool isActive)
    {
        var schedule =
            new WorkSchedule(
                Guid.NewGuid(),
                code,
                $"Lịch {code}",
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

    private static async Task<HolidayCalendarDay> SeedHolidayAsync(
        TestDatabase database,
        DateOnly date,
        string name,
        bool isActive)
    {
        var holiday =
            new HolidayCalendarDay(
                Guid.NewGuid(),
                date,
                name,
                isActive);

        await using HrManagementDbContext dbContext =
            await database.Factory
                .CreateDbContextAsync();

        dbContext.HolidayCalendarDays.Add(
            holiday);

        await dbContext.SaveChangesAsync();

        return holiday;
    }

    private static async Task<WorkScheduleDateOverride>
        SeedOverrideAsync(
            TestDatabase database,
            Guid scheduleId,
            DateOnly workDate)
    {
        var dateOverride =
            new WorkScheduleDateOverride(
                Guid.NewGuid(),
                scheduleId,
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
            dateOverride);

        await dbContext.SaveChangesAsync();

        return dateOverride;
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

        public EfHolidayExceptionWorkspaceQueryService Service
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

            Service =
                new EfHolidayExceptionWorkspaceQueryService(
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
