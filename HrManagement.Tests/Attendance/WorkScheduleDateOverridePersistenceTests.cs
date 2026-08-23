using HrManagement.Domain.Attendance.Schedules;
using HrManagement.Infrastructure.Attendance.Schedules.Overrides;
using HrManagement.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace HrManagement.Tests.Attendance;

public sealed class WorkScheduleDateOverridePersistenceTests
{
    [Fact]
    public async Task CreateAndGetByIdAsync_RoundTripsWorkingOverride()
    {
        await using TestDatabase database =
            await TestDatabase.CreateAsync();

        WorkSchedule schedule =
            await SeedScheduleAsync(
                database);

        var item =
            new WorkScheduleDateOverride(
                Guid.NewGuid(),
                schedule.Id,
                new DateOnly(
                    2026,
                    9,
                    5),
                true,
                new TimeOnly(
                    8,
                    0),
                new TimeOnly(
                    17,
                    0),
                60,
                "Làm bù");

        await database.Persistence
            .CreateAsync(
                item);

        WorkScheduleDateOverride loaded =
            Assert.IsType<WorkScheduleDateOverride>(
                await database.Persistence
                    .GetByIdAsync(
                        item.Id));

        Assert.Equal(
            item.Id,
            loaded.Id);

        Assert.Equal(
            schedule.Id,
            loaded.WorkScheduleId);

        Assert.Equal(
            item.WorkDate,
            loaded.WorkDate);

        Assert.True(
            loaded.IsWorkingDay);

        Assert.Equal(
            new TimeOnly(
                8,
                0),
            loaded.StartTime);

        Assert.Equal(
            new TimeOnly(
                17,
                0),
            loaded.EndTime);

        Assert.Equal(
            60,
            loaded.BreakMinutes);

        Assert.Equal(
            480,
            loaded.PlannedMinutes);

        Assert.Equal(
            "Làm bù",
            loaded.Note);
    }

    [Fact]
    public async Task GetByScheduleAndDateAsync_ReturnsNonWorkingOverride()
    {
        await using TestDatabase database =
            await TestDatabase.CreateAsync();

        WorkSchedule schedule =
            await SeedScheduleAsync(
                database);

        var item =
            new WorkScheduleDateOverride(
                Guid.NewGuid(),
                schedule.Id,
                new DateOnly(
                    2026,
                    9,
                    2),
                false,
                note:
                    "Nghỉ điều chỉnh");

        await database.Persistence
            .CreateAsync(
                item);

        WorkScheduleDateOverride loaded =
            Assert.IsType<WorkScheduleDateOverride>(
                await database.Persistence
                    .GetByScheduleAndDateAsync(
                        schedule.Id,
                        item.WorkDate));

        Assert.Equal(
            item.Id,
            loaded.Id);

        Assert.False(
            loaded.IsWorkingDay);

        Assert.Null(
            loaded.StartTime);

        Assert.Null(
            loaded.EndTime);

        Assert.Equal(
            0,
            loaded.BreakMinutes);
    }

    [Fact]
    public async Task UpdateAsync_PersistsChangedExpectation()
    {
        await using TestDatabase database =
            await TestDatabase.CreateAsync();

        WorkSchedule schedule =
            await SeedScheduleAsync(
                database);

        var original =
            new WorkScheduleDateOverride(
                Guid.NewGuid(),
                schedule.Id,
                new DateOnly(
                    2026,
                    9,
                    2),
                false);

        await database.Persistence
            .CreateAsync(
                original);

        var updated =
            new WorkScheduleDateOverride(
                original.Id,
                original.WorkScheduleId,
                original.WorkDate,
                true,
                new TimeOnly(
                    22,
                    0),
                new TimeOnly(
                    6,
                    0),
                30,
                "Trực ngày lễ");

        await database.Persistence
            .UpdateAsync(
                updated);

        WorkScheduleDateOverride loaded =
            Assert.IsType<WorkScheduleDateOverride>(
                await database.Persistence
                    .GetByIdAsync(
                        original.Id));

        Assert.True(
            loaded.IsWorkingDay);

        Assert.True(
            loaded.IsOvernight);

        Assert.Equal(
            450,
            loaded.PlannedMinutes);

        Assert.Equal(
            "Trực ngày lễ",
            loaded.Note);
    }

    [Fact]
    public async Task DeleteAsync_RemovesOverride()
    {
        await using TestDatabase database =
            await TestDatabase.CreateAsync();

        WorkSchedule schedule =
            await SeedScheduleAsync(
                database);

        var item =
            new WorkScheduleDateOverride(
                Guid.NewGuid(),
                schedule.Id,
                new DateOnly(
                    2026,
                    9,
                    5),
                false);

        await database.Persistence
            .CreateAsync(
                item);

        await database.Persistence
            .DeleteAsync(
                item.Id);

        WorkScheduleDateOverride? loaded =
            await database.Persistence
                .GetByIdAsync(
                    item.Id);

        Assert.Null(
            loaded);
    }

    [Fact]
    public async Task CreateAsync_WithDuplicateScheduleAndDate_Throws()
    {
        await using TestDatabase database =
            await TestDatabase.CreateAsync();

        WorkSchedule schedule =
            await SeedScheduleAsync(
                database);

        DateOnly workDate =
            new(
                2026,
                9,
                5);

        await database.Persistence
            .CreateAsync(
                new WorkScheduleDateOverride(
                    Guid.NewGuid(),
                    schedule.Id,
                    workDate,
                    false));

        var duplicate =
            new WorkScheduleDateOverride(
                Guid.NewGuid(),
                schedule.Id,
                workDate,
                true,
                new TimeOnly(
                    8,
                    0),
                new TimeOnly(
                    17,
                    0),
                60);

        await Assert.ThrowsAsync<DbUpdateException>(
            () =>
                database.Persistence
                    .CreateAsync(
                        duplicate));
    }

    [Fact]
    public async Task DeletingUnusedSchedule_CascadesItsOverrides()
    {
        await using TestDatabase database =
            await TestDatabase.CreateAsync();

        WorkSchedule schedule =
            await SeedScheduleAsync(
                database);

        var item =
            new WorkScheduleDateOverride(
                Guid.NewGuid(),
                schedule.Id,
                new DateOnly(
                    2026,
                    9,
                    5),
                false);

        await database.Persistence
            .CreateAsync(
                item);

        await using HrManagementDbContext dbContext =
            await database.Factory
                .CreateDbContextAsync();

        WorkSchedule loadedSchedule =
            await dbContext
                .WorkSchedules
                .SingleAsync(
                    value =>
                        value.Id ==
                        schedule.Id);

        dbContext.WorkSchedules.Remove(
            loadedSchedule);

        await dbContext.SaveChangesAsync();

        WorkScheduleDateOverride? remaining =
            await database.Persistence
                .GetByIdAsync(
                    item.Id);

        Assert.Null(
            remaining);
    }

    private static async Task<WorkSchedule> SeedScheduleAsync(
        TestDatabase database)
    {
        var schedule =
            new WorkSchedule(
                Guid.NewGuid(),
                "TEST",
                "Lịch thử nghiệm",
                "SE Asia Standard Time",
                isActive: false);

        await using HrManagementDbContext dbContext =
            await database.Factory
                .CreateDbContextAsync();

        dbContext.WorkSchedules.Add(
            schedule);

        await dbContext.SaveChangesAsync();

        return schedule;
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

        public EfWorkScheduleDateOverrideManagementPersistence Persistence
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
                new EfWorkScheduleDateOverrideManagementPersistence(
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
