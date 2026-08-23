using HrManagement.Domain.Attendance.Calendars;
using HrManagement.Infrastructure.Attendance.Calendars;
using HrManagement.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace HrManagement.Tests.Attendance;

public sealed class HolidayCalendarPersistenceTests
{
    [Fact]
    public async Task CreateAndGetByIdAsync_RoundTripsHoliday()
    {
        await using TestDatabase database =
            await TestDatabase.CreateAsync();

        var holiday =
            new HolidayCalendarDay(
                Guid.NewGuid(),
                new DateOnly(
                    2026,
                    9,
                    2),
                "Quốc khánh");

        await database.Persistence
            .CreateAsync(
                holiday);

        HolidayCalendarDay? loaded =
            await database.Persistence
                .GetByIdAsync(
                    holiday.Id);

        Assert.NotNull(
            loaded);

        Assert.Equal(
            holiday.Id,
            loaded.Id);

        Assert.Equal(
            holiday.Date,
            loaded.Date);

        Assert.Equal(
            "Quốc khánh",
            loaded.Name);

        Assert.True(
            loaded.IsActive);
    }

    [Fact]
    public async Task GetByDateAsync_ReturnsInactiveHoliday()
    {
        await using TestDatabase database =
            await TestDatabase.CreateAsync();

        var holiday =
            new HolidayCalendarDay(
                Guid.NewGuid(),
                new DateOnly(
                    2026,
                    9,
                    2),
                "Quốc khánh",
                isActive: false);

        await database.Persistence
            .CreateAsync(
                holiday);

        HolidayCalendarDay? loaded =
            await database.Persistence
                .GetByDateAsync(
                    holiday.Date);

        Assert.NotNull(
            loaded);

        Assert.Equal(
            holiday.Id,
            loaded.Id);

        Assert.False(
            loaded.IsActive);
    }

    [Fact]
    public async Task UpdateAsync_PersistsRenameAndActiveState()
    {
        await using TestDatabase database =
            await TestDatabase.CreateAsync();

        var holiday =
            new HolidayCalendarDay(
                Guid.NewGuid(),
                new DateOnly(
                    2026,
                    9,
                    2),
                "Quốc khánh");

        await database.Persistence
            .CreateAsync(
                holiday);

        HolidayCalendarDay loaded =
            Assert.IsType<HolidayCalendarDay>(
                await database.Persistence
                    .GetByIdAsync(
                        holiday.Id));

        loaded.Rename(
            "Quốc khánh Việt Nam");

        loaded.Deactivate();

        await database.Persistence
            .UpdateAsync(
                loaded);

        HolidayCalendarDay updated =
            Assert.IsType<HolidayCalendarDay>(
                await database.Persistence
                    .GetByIdAsync(
                        holiday.Id));

        Assert.Equal(
            "Quốc khánh Việt Nam",
            updated.Name);

        Assert.False(
            updated.IsActive);
    }

    [Fact]
    public async Task CreateAsync_WithDuplicateDate_ThrowsDatabaseUpdateException()
    {
        await using TestDatabase database =
            await TestDatabase.CreateAsync();

        DateOnly date =
            new(
                2026,
                9,
                2);

        await database.Persistence
            .CreateAsync(
                new HolidayCalendarDay(
                    Guid.NewGuid(),
                    date,
                    "Ngày lễ thứ nhất"));

        var duplicate =
            new HolidayCalendarDay(
                Guid.NewGuid(),
                date,
                "Ngày lễ thứ hai");

        await Assert.ThrowsAsync<DbUpdateException>(
            () =>
                database.Persistence
                    .CreateAsync(
                        duplicate));
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

        public EfHolidayCalendarManagementPersistence Persistence
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
                new EfHolidayCalendarManagementPersistence(
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
