using HrManagement.Application.Workspaces.WorkSchedules;
using HrManagement.Domain.Attendance.Schedules;
using HrManagement.Domain.Employees;
using HrManagement.Infrastructure.Persistence;
using HrManagement.Infrastructure.Workspaces.WorkSchedules;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace HrManagement.Tests.Attendance;

public sealed class WorkScheduleWorkspaceQueryServiceTests
{
    [Fact]
    public async Task GetEmployeesAsync_ReturnsEmployeesOrderedByCode()
    {
        await using SqliteConnection connection =
            await CreateOpenConnectionAsync();

        DbContextOptions<HrManagementDbContext> options =
            CreateOptions(
                connection);

        await EnsureCreatedAsync(
            options);

        await AddEmployeeAsync(
            options,
            "EMP-B",
            "Nhân viên B");

        await AddEmployeeAsync(
            options,
            "EMP-A",
            "Nhân viên A");

        IWorkScheduleWorkspaceQueryService service =
            CreateService(
                options);

        IReadOnlyList<WorkScheduleWorkspaceEmployeeItem> result =
            await service.GetEmployeesAsync();

        Assert.Equal(
            2,
            result.Count);

        Assert.Equal(
            "EMP-A",
            result[0].EmployeeCode);

        Assert.Equal(
            "EMP-B",
            result[1].EmployeeCode);
    }

    [Fact]
    public async Task GetAsync_ReturnsScheduleAndDayProjections()
    {
        await using SqliteConnection connection =
            await CreateOpenConnectionAsync();

        DbContextOptions<HrManagementDbContext> options =
            CreateOptions(
                connection);

        await EnsureCreatedAsync(
            options);

        Guid scheduleId =
            Guid.NewGuid();

        await using (
            var dbContext =
                new HrManagementDbContext(
                    options))
        {
            await dbContext.WorkSchedules.AddAsync(
                new WorkSchedule(
                    scheduleId,
                    "OFFICE",
                    "Giờ hành chính",
                    "SE Asia Standard Time"));

            await dbContext.WorkScheduleDays.AddRangeAsync(
                new WorkScheduleDay(
                    Guid.NewGuid(),
                    scheduleId,
                    DayOfWeek.Monday,
                    isWorkingDay: true,
                    startTime:
                        new TimeOnly(
                            8,
                            0),
                    endTime:
                        new TimeOnly(
                            17,
                            0),
                    breakMinutes:
                        60),

                new WorkScheduleDay(
                    Guid.NewGuid(),
                    scheduleId,
                    DayOfWeek.Sunday,
                    isWorkingDay: false));

            await dbContext.SaveChangesAsync();
        }

        IWorkScheduleWorkspaceQueryService service =
            CreateService(
                options);

        WorkScheduleWorkspaceSnapshot result =
            await service.GetAsync(
                new WorkScheduleWorkspaceQuery());

        WorkScheduleWorkspaceScheduleItem schedule =
            Assert.Single(
                result.Schedules);

        Assert.Equal(
            scheduleId,
            schedule.WorkScheduleId);

        Assert.Equal(
            "OFFICE",
            schedule.Code);

        Assert.Equal(
            "Giờ hành chính",
            schedule.Name);

        Assert.True(
            schedule.IsActive);

        Assert.Equal(
            2,
            result.ScheduleDays.Count);

        WorkScheduleWorkspaceDayItem monday =
            Assert.Single(
                result.ScheduleDays.Where(
                    day =>
                        day.DayOfWeek ==
                        DayOfWeek.Monday));

        Assert.True(
            monday.IsWorkingDay);

        Assert.Equal(
            new TimeOnly(
                8,
                0),
            monday.StartTime);

        Assert.Equal(
            new TimeOnly(
                17,
                0),
            monday.EndTime);

        Assert.Equal(
            60,
            monday.BreakMinutes);

        Assert.Equal(
            480,
            monday.PlannedMinutes);
    }

    [Fact]
    public async Task GetAsync_ReturnsAssignmentWithEmployeeAndSchedule()
    {
        await using SqliteConnection connection =
            await CreateOpenConnectionAsync();

        DbContextOptions<HrManagementDbContext> options =
            CreateOptions(
                connection);

        await EnsureCreatedAsync(
            options);

        SeedContext seed =
            await SeedAssignmentAsync(
                options,
                "A");

        IWorkScheduleWorkspaceQueryService service =
            CreateService(
                options);

        WorkScheduleWorkspaceSnapshot result =
            await service.GetAsync(
                new WorkScheduleWorkspaceQuery());

        WorkScheduleWorkspaceAssignmentItem assignment =
            Assert.Single(
                result.Assignments);

        Assert.Equal(
            seed.AssignmentId,
            assignment.AssignmentId);

        Assert.Equal(
            seed.EmployeeId,
            assignment.EmployeeId);

        Assert.Equal(
            "EMP-A",
            assignment.EmployeeCode);

        Assert.Equal(
            "Nhân viên A",
            assignment.EmployeeName);

        Assert.Equal(
            seed.EmploymentPeriodId,
            assignment.EmploymentPeriodId);

        Assert.Equal(
            seed.ScheduleId,
            assignment.WorkScheduleId);

        Assert.Equal(
            "WS-A",
            assignment.WorkScheduleCode);

        Assert.Equal(
            "Lịch A",
            assignment.WorkScheduleName);

        Assert.Equal(
            new DateOnly(
                2026,
                1,
                1),
            assignment.EffectiveFrom);

        Assert.Null(
            assignment.EffectiveTo);

        Assert.True(
            assignment.IsOpen);
    }

    [Fact]
    public async Task GetAsync_EmployeeFilterReturnsOnlySelectedEmployeeAssignments()
    {
        await using SqliteConnection connection =
            await CreateOpenConnectionAsync();

        DbContextOptions<HrManagementDbContext> options =
            CreateOptions(
                connection);

        await EnsureCreatedAsync(
            options);

        SeedContext first =
            await SeedAssignmentAsync(
                options,
                "A");

        await SeedAssignmentAsync(
            options,
            "B");

        IWorkScheduleWorkspaceQueryService service =
            CreateService(
                options);

        WorkScheduleWorkspaceSnapshot result =
            await service.GetAsync(
                new WorkScheduleWorkspaceQuery(
                    first.EmployeeId));

        WorkScheduleWorkspaceAssignmentItem assignment =
            Assert.Single(
                result.Assignments);

        Assert.Equal(
            first.EmployeeId,
            assignment.EmployeeId);

        Assert.Equal(
            "EMP-A",
            assignment.EmployeeCode);

        Assert.Equal(
            2,
            result.Schedules.Count);
    }

    [Fact]
    public async Task GetAsync_EmptyEmployeeIdThrows()
    {
        await using SqliteConnection connection =
            await CreateOpenConnectionAsync();

        DbContextOptions<HrManagementDbContext> options =
            CreateOptions(
                connection);

        await EnsureCreatedAsync(
            options);

        IWorkScheduleWorkspaceQueryService service =
            CreateService(
                options);

        await Assert.ThrowsAsync<ArgumentException>(
            () =>
                service.GetAsync(
                    new WorkScheduleWorkspaceQuery(
                        Guid.Empty)));
    }

    private static IWorkScheduleWorkspaceQueryService
        CreateService(
            DbContextOptions<HrManagementDbContext> options)
    {
        return new EfWorkScheduleWorkspaceQueryService(
            new TestDbContextFactory(
                options));
    }

    private static async Task<Guid> AddEmployeeAsync(
        DbContextOptions<HrManagementDbContext> options,
        string employeeCode,
        string employeeName)
    {
        Guid employeeId =
            Guid.NewGuid();

        await using var dbContext =
            new HrManagementDbContext(
                options);

        await dbContext.Employees.AddAsync(
            new Employee(
                employeeId,
                employeeCode,
                employeeName,
                null,
                null,
                null,
                new DateOnly(
                    2026,
                    1,
                    1),
                "Phòng kiểm thử",
                "Chuyên viên",
                EmployeeStatus.Active));

        await dbContext.SaveChangesAsync();

        return employeeId;
    }

    private static async Task<SeedContext>
        SeedAssignmentAsync(
            DbContextOptions<HrManagementDbContext> options,
            string suffix)
    {
        Guid employeeId =
            Guid.NewGuid();

        Guid employmentPeriodId =
            Guid.NewGuid();

        Guid scheduleId =
            Guid.NewGuid();

        Guid assignmentId =
            Guid.NewGuid();

        await using var dbContext =
            new HrManagementDbContext(
                options);

        await dbContext.Employees.AddAsync(
            new Employee(
                employeeId,
                $"EMP-{suffix}",
                $"Nhân viên {suffix}",
                null,
                null,
                null,
                new DateOnly(
                    2026,
                    1,
                    1),
                "Phòng kiểm thử",
                "Chuyên viên",
                EmployeeStatus.Active));

        await dbContext.EmploymentPeriods.AddAsync(
            new EmploymentPeriod(
                employmentPeriodId,
                employeeId,
                new DateOnly(
                    2026,
                    1,
                    1)));

        await dbContext.WorkSchedules.AddAsync(
            new WorkSchedule(
                scheduleId,
                $"WS-{suffix}",
                $"Lịch {suffix}",
                "SE Asia Standard Time"));

        await dbContext
            .EmployeeWorkScheduleAssignments
            .AddAsync(
                new EmployeeWorkScheduleAssignment(
                    assignmentId,
                    employeeId,
                    employmentPeriodId,
                    scheduleId,
                    new DateOnly(
                        2026,
                        1,
                        1)));

        await dbContext.SaveChangesAsync();

        return new SeedContext(
            employeeId,
            employmentPeriodId,
            scheduleId,
            assignmentId);
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
        return new DbContextOptionsBuilder<HrManagementDbContext>()
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

        await dbContext.Database.EnsureCreatedAsync();
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

    private sealed record SeedContext(
        Guid EmployeeId,
        Guid EmploymentPeriodId,
        Guid ScheduleId,
        Guid AssignmentId);
}
