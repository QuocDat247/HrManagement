using HrManagement.Domain.Attendance.Timesheets;
using HrManagement.Infrastructure.Attendance.Timesheets;
using HrManagement.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace HrManagement.Tests.Attendance;

public sealed class AttendancePeriodLockPolicyTests
{
    [Fact]
    public async Task IsLockedAsync_WhenPeriodDoesNotExist_ReturnsFalse()
    {
        await using TestDatabase database =
            await TestDatabase.CreateAsync();

        bool result =
            await database.Policy.IsLockedAsync(
                new DateOnly(
                    2026,
                    8,
                    15));

        Assert.False(
            result);
    }

    [Fact]
    public async Task IsLockedAsync_WhenPeriodIsOpen_ReturnsFalse()
    {
        await using TestDatabase database =
            await TestDatabase.CreateAsync();

        var period =
            new TimesheetPeriod(
                Guid.NewGuid(),
                2026,
                8);

        await database.AddPeriodAsync(
            period);

        bool result =
            await database.Policy.IsLockedAsync(
                new DateOnly(
                    2026,
                    8,
                    31));

        Assert.False(
            result);
    }

    [Fact]
    public async Task IsLockedAsync_WhenPeriodIsClosed_ReturnsTrue()
    {
        await using TestDatabase database =
            await TestDatabase.CreateAsync();

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

        await database.AddPeriodAsync(
            period);

        bool result =
            await database.Policy.IsLockedAsync(
                new DateOnly(
                    2026,
                    8,
                    1));

        Assert.True(
            result);
    }

    [Fact]
    public async Task IsLockedAsync_UsesRequestedMonthOnly()
    {
        await using TestDatabase database =
            await TestDatabase.CreateAsync();

        var august =
            new TimesheetPeriod(
                Guid.NewGuid(),
                2026,
                8);

        august.Close(
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

        await database.AddPeriodAsync(
            august);

        bool septemberLocked =
            await database.Policy.IsLockedAsync(
                new DateOnly(
                    2026,
                    9,
                    1));

        Assert.False(
            septemberLocked);
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

        public EfAttendancePeriodLockPolicy Policy
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

            Policy =
                new EfAttendancePeriodLockPolicy(
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

        public async Task AddPeriodAsync(
            TimesheetPeriod period)
        {
            await using HrManagementDbContext dbContext =
                await Factory.CreateDbContextAsync();

            dbContext.TimesheetPeriods.Add(
                period);

            await dbContext.SaveChangesAsync();
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
