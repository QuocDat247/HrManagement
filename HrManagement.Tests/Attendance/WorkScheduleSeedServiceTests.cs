using HrManagement.Domain.Attendance.Schedules;
using HrManagement.Infrastructure.Attendance.Schedules;
using HrManagement.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace HrManagement.Tests.Attendance;

public sealed class WorkScheduleSeedServiceTests
{
    [Fact]
    public async Task SeedAsync_WhenScheduleDoesNotExist_CreatesDefaultOfficeSchedule()
    {
        await using SqliteConnection connection =
            await CreateOpenConnectionAsync();

        DbContextOptions<HrManagementDbContext> options =
            CreateOptions(
                connection);

        await EnsureCreatedAsync(
            options);

        var service =
            CreateService(
                options);

        await service.SeedAsync();

        await using var dbContext =
            new HrManagementDbContext(
                options);

        WorkSchedule schedule =
            await dbContext
                .WorkSchedules
                .AsNoTracking()
                .SingleAsync();

        Assert.Equal(
            WorkScheduleSeedService.DefaultOfficeScheduleId,
            schedule.Id);

        Assert.Equal(
            "OFFICE",
            schedule.Code);

        Assert.Equal(
            "Giờ hành chính",
            schedule.Name);

        Assert.Equal(
            "SE Asia Standard Time",
            schedule.TimeZoneId);

        Assert.True(
            schedule.IsActive);
    }

    [Fact]
    public async Task SeedAsync_CreatesSevenDayDefinitions()
    {
        await using SqliteConnection connection =
            await CreateOpenConnectionAsync();

        DbContextOptions<HrManagementDbContext> options =
            CreateOptions(
                connection);

        await EnsureCreatedAsync(
            options);

        var service =
            CreateService(
                options);

        await service.SeedAsync();

        await using var dbContext =
            new HrManagementDbContext(
                options);

        List<WorkScheduleDay> days =
            await dbContext
                .WorkScheduleDays
                .AsNoTracking()
                .Where(
                    day =>
                        day.WorkScheduleId ==
                        WorkScheduleSeedService
                            .DefaultOfficeScheduleId)
                .ToListAsync();

        Assert.Equal(
            7,
            days.Count);

        for (DayOfWeek dayOfWeek =
                 DayOfWeek.Monday;
             dayOfWeek <= DayOfWeek.Friday;
             dayOfWeek++)
        {
            WorkScheduleDay day =
                Assert.Single(
                    days.Where(
                        item =>
                            item.DayOfWeek ==
                            dayOfWeek));

            Assert.True(
                day.IsWorkingDay);

            Assert.Equal(
                new TimeOnly(
                    8,
                    0),
                day.StartTime);

            Assert.Equal(
                new TimeOnly(
                    17,
                    0),
                day.EndTime);

            Assert.Equal(
                60,
                day.BreakMinutes);

            Assert.Equal(
                480,
                day.PlannedMinutes);
        }

        WorkScheduleDay saturday =
            Assert.Single(
                days.Where(
                    day =>
                        day.DayOfWeek ==
                        DayOfWeek.Saturday));

        WorkScheduleDay sunday =
            Assert.Single(
                days.Where(
                    day =>
                        day.DayOfWeek ==
                        DayOfWeek.Sunday));

        Assert.False(
            saturday.IsWorkingDay);

        Assert.False(
            sunday.IsWorkingDay);
    }

    [Fact]
    public async Task SeedAsync_WhenCalledTwice_IsIdempotent()
    {
        await using SqliteConnection connection =
            await CreateOpenConnectionAsync();

        DbContextOptions<HrManagementDbContext> options =
            CreateOptions(
                connection);

        await EnsureCreatedAsync(
            options);

        var service =
            CreateService(
                options);

        await service.SeedAsync();
        await service.SeedAsync();

        await using var dbContext =
            new HrManagementDbContext(
                options);

        Assert.Equal(
            1,
            await dbContext
                .WorkSchedules
                .CountAsync(
                    schedule =>
                        schedule.Code ==
                        "OFFICE"));

        Assert.Equal(
            7,
            await dbContext
                .WorkScheduleDays
                .CountAsync());
    }

    [Fact]
    public async Task SeedAsync_WhenOfficeScheduleAlreadyExists_DoesNotOverwriteIt()
    {
        await using SqliteConnection connection =
            await CreateOpenConnectionAsync();

        DbContextOptions<HrManagementDbContext> options =
            CreateOptions(
                connection);

        await EnsureCreatedAsync(
            options);

        Guid existingId =
            Guid.NewGuid();

        await using (
            var seedContext =
                new HrManagementDbContext(
                    options))
        {
            await seedContext.WorkSchedules.AddAsync(
                new WorkSchedule(
                    existingId,
                    "OFFICE",
                    "Lịch tùy chỉnh",
                    "Custom Time Zone"));

            await seedContext.SaveChangesAsync();
        }

        var service =
            CreateService(
                options);

        await service.SeedAsync();

        await using var verificationContext =
            new HrManagementDbContext(
                options);

        WorkSchedule schedule =
            await verificationContext
                .WorkSchedules
                .AsNoTracking()
                .SingleAsync();

        Assert.Equal(
            existingId,
            schedule.Id);

        Assert.Equal(
            "Lịch tùy chỉnh",
            schedule.Name);

        Assert.Equal(
            "Custom Time Zone",
            schedule.TimeZoneId);

        Assert.Empty(
            await verificationContext
                .WorkScheduleDays
                .AsNoTracking()
                .ToListAsync());
    }

    private static WorkScheduleSeedService CreateService(
        DbContextOptions<HrManagementDbContext> options)
    {
        return new WorkScheduleSeedService(
            new TestDbContextFactory(
                options));
    }

    private static async Task<SqliteConnection>
        CreateOpenConnectionAsync()
    {
        var connection =
            new SqliteConnection(
                "Data Source=:memory:");

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
