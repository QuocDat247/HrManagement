using HrManagement.Domain.Attendance.Schedules;
using HrManagement.Domain.Employees;
using HrManagement.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace HrManagement.Tests.Attendance;

public sealed class WorkSchedulePersistenceTests
{
    [Fact]
    public async Task ScheduleAndDays_RoundTrip_PreservesWorkingDefinitions()
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
                    "Văn phòng",
                    "SE Asia Standard Time"));

            await dbContext.WorkScheduleDays.AddRangeAsync(
                new WorkScheduleDay(
                    Guid.NewGuid(),
                    scheduleId,
                    DayOfWeek.Monday,
                    true,
                    new TimeOnly(
                        8,
                        0),
                    new TimeOnly(
                        17,
                        0),
                    breakMinutes:
                        60),
                new WorkScheduleDay(
                    Guid.NewGuid(),
                    scheduleId,
                    DayOfWeek.Sunday,
                    false));

            await dbContext.SaveChangesAsync();
        }

        await using var verificationContext =
            new HrManagementDbContext(
                options);

        WorkSchedule schedule =
            await verificationContext
                .WorkSchedules
                .AsNoTracking()
                .SingleAsync();

        List<WorkScheduleDay> days =
            await verificationContext
                .WorkScheduleDays
                .AsNoTracking()
                .OrderBy(
                    day =>
                        day.DayOfWeek)
                .ToListAsync();

        Assert.Equal(
            scheduleId,
            schedule.Id);

        Assert.Equal(
            "OFFICE",
            schedule.Code);

        Assert.Equal(
            "Văn phòng",
            schedule.Name);

        Assert.Equal(
            2,
            days.Count);

        WorkScheduleDay monday =
            Assert.Single(
                days.Where(
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

        WorkScheduleDay sunday =
            Assert.Single(
                days.Where(
                    day =>
                        day.DayOfWeek ==
                        DayOfWeek.Sunday));

        Assert.False(
            sunday.IsWorkingDay);

        Assert.Equal(
            0,
            sunday.PlannedMinutes);
    }

    [Fact]
    public async Task OvernightScheduleDay_RoundTrip_PreservesOvernightMeaning()
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
                    "NIGHT",
                    "Ca đêm",
                    "SE Asia Standard Time"));

            await dbContext.WorkScheduleDays.AddAsync(
                new WorkScheduleDay(
                    Guid.NewGuid(),
                    scheduleId,
                    DayOfWeek.Monday,
                    true,
                    new TimeOnly(
                        22,
                        0),
                    new TimeOnly(
                        6,
                        0),
                    breakMinutes:
                        60));

            await dbContext.SaveChangesAsync();
        }

        await using var verificationContext =
            new HrManagementDbContext(
                options);

        WorkScheduleDay saved =
            await verificationContext
                .WorkScheduleDays
                .AsNoTracking()
                .SingleAsync();

        Assert.True(
            saved.IsOvernight);

        Assert.Equal(
            420,
            saved.PlannedMinutes);
    }

    [Fact]
    public async Task ScheduleCode_MustBeUnique()
    {
        await using SqliteConnection connection =
            await CreateOpenConnectionAsync();

        DbContextOptions<HrManagementDbContext> options =
            CreateOptions(
                connection);

        await EnsureCreatedAsync(
            options);

        await using var dbContext =
            new HrManagementDbContext(
                options);

        await dbContext.WorkSchedules.AddAsync(
            new WorkSchedule(
                Guid.NewGuid(),
                "OFFICE",
                "Văn phòng 1",
                "SE Asia Standard Time"));

        await dbContext.SaveChangesAsync();

        await dbContext.WorkSchedules.AddAsync(
            new WorkSchedule(
                Guid.NewGuid(),
                "OFFICE",
                "Văn phòng 2",
                "SE Asia Standard Time"));

        await Assert.ThrowsAsync<DbUpdateException>(
            () =>
                dbContext.SaveChangesAsync());
    }

    [Fact]
    public async Task ScheduleDay_DayOfWeekMustBeUniqueWithinSchedule()
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

        await using var dbContext =
            new HrManagementDbContext(
                options);

        await dbContext.WorkSchedules.AddAsync(
            new WorkSchedule(
                scheduleId,
                "OFFICE",
                "Văn phòng",
                "SE Asia Standard Time"));

        await dbContext.WorkScheduleDays.AddRangeAsync(
            new WorkScheduleDay(
                Guid.NewGuid(),
                scheduleId,
                DayOfWeek.Monday,
                true,
                new TimeOnly(
                    8,
                    0),
                new TimeOnly(
                    17,
                    0)),
            new WorkScheduleDay(
                Guid.NewGuid(),
                scheduleId,
                DayOfWeek.Monday,
                true,
                new TimeOnly(
                    9,
                    0),
                new TimeOnly(
                    18,
                    0)));

        await Assert.ThrowsAsync<DbUpdateException>(
            () =>
                dbContext.SaveChangesAsync());
    }

    [Fact]
    public async Task Assignment_RoundTrip_PreservesTimelineReferences()
    {
        await using SqliteConnection connection =
            await CreateOpenConnectionAsync();

        DbContextOptions<HrManagementDbContext> options =
            CreateOptions(
                connection);

        await EnsureCreatedAsync(
            options);

        Guid employeeId =
            Guid.NewGuid();

        Guid periodId =
            Guid.NewGuid();

        Guid scheduleId =
            Guid.NewGuid();

        Guid assignmentId =
            Guid.NewGuid();

        await SeedEmployeePeriodAndScheduleAsync(
            options,
            employeeId,
            periodId,
            scheduleId);

        await using (
            var dbContext =
                new HrManagementDbContext(
                    options))
        {
            await dbContext
                .EmployeeWorkScheduleAssignments
                .AddAsync(
                    new EmployeeWorkScheduleAssignment(
                        assignmentId,
                        employeeId,
                        periodId,
                        scheduleId,
                        new DateOnly(
                            2026,
                            8,
                            1)));

            await dbContext.SaveChangesAsync();
        }

        await using var verificationContext =
            new HrManagementDbContext(
                options);

        EmployeeWorkScheduleAssignment saved =
            await verificationContext
                .EmployeeWorkScheduleAssignments
                .AsNoTracking()
                .SingleAsync();

        Assert.Equal(
            assignmentId,
            saved.Id);

        Assert.Equal(
            employeeId,
            saved.EmployeeId);

        Assert.Equal(
            periodId,
            saved.EmploymentPeriodId);

        Assert.Equal(
            scheduleId,
            saved.WorkScheduleId);

        Assert.Equal(
            new DateOnly(
                2026,
                8,
                1),
            saved.EffectiveFrom);

        Assert.Null(
            saved.EffectiveTo);

        Assert.True(
            saved.IsOpen);
    }

    [Fact]
    public async Task Assignment_AllowsOnlyOneOpenAssignmentPerEmployee()
    {
        await using SqliteConnection connection =
            await CreateOpenConnectionAsync();

        DbContextOptions<HrManagementDbContext> options =
            CreateOptions(
                connection);

        await EnsureCreatedAsync(
            options);

        Guid employeeId =
            Guid.NewGuid();

        Guid periodId =
            Guid.NewGuid();

        Guid firstScheduleId =
            Guid.NewGuid();

        Guid secondScheduleId =
            Guid.NewGuid();

        await AddEmployeeAsync(
            options,
            employeeId);

        await using (
            var seedContext =
                new HrManagementDbContext(
                    options))
        {
            await seedContext.EmploymentPeriods.AddAsync(
                new EmploymentPeriod(
                    periodId,
                    employeeId,
                    new DateOnly(
                        2026,
                        1,
                        1)));

            await seedContext.WorkSchedules.AddRangeAsync(
                new WorkSchedule(
                    firstScheduleId,
                    "OFFICE",
                    "Văn phòng",
                    "SE Asia Standard Time"),
                new WorkSchedule(
                    secondScheduleId,
                    "NIGHT",
                    "Ca đêm",
                    "SE Asia Standard Time"));

            await seedContext.SaveChangesAsync();
        }

        await using var dbContext =
            new HrManagementDbContext(
                options);

        await dbContext
            .EmployeeWorkScheduleAssignments
            .AddAsync(
                new EmployeeWorkScheduleAssignment(
                    Guid.NewGuid(),
                    employeeId,
                    periodId,
                    firstScheduleId,
                    new DateOnly(
                        2026,
                        1,
                        1)));

        await dbContext.SaveChangesAsync();

        await dbContext
            .EmployeeWorkScheduleAssignments
            .AddAsync(
                new EmployeeWorkScheduleAssignment(
                    Guid.NewGuid(),
                    employeeId,
                    periodId,
                    secondScheduleId,
                    new DateOnly(
                        2026,
                        9,
                        1)));

        await Assert.ThrowsAsync<DbUpdateException>(
            () =>
                dbContext.SaveChangesAsync());
    }

    [Fact]
    public async Task DeletingScheduleWithoutAssignments_CascadesScheduleDays()
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
            var seedContext =
                new HrManagementDbContext(
                    options))
        {
            await seedContext.WorkSchedules.AddAsync(
                new WorkSchedule(
                    scheduleId,
                    "TEMP",
                    "Lịch tạm",
                    "SE Asia Standard Time"));

            await seedContext.WorkScheduleDays.AddAsync(
                new WorkScheduleDay(
                    Guid.NewGuid(),
                    scheduleId,
                    DayOfWeek.Monday,
                    true,
                    new TimeOnly(
                        8,
                        0),
                    new TimeOnly(
                        17,
                        0)));

            await seedContext.SaveChangesAsync();
        }

        await using (
            var deleteContext =
                new HrManagementDbContext(
                    options))
        {
            WorkSchedule schedule =
                await deleteContext
                    .WorkSchedules
                    .SingleAsync(
                        item =>
                            item.Id ==
                            scheduleId);

            deleteContext.WorkSchedules.Remove(
                schedule);

            await deleteContext.SaveChangesAsync();
        }

        await using var verificationContext =
            new HrManagementDbContext(
                options);

        Assert.Empty(
            await verificationContext
                .WorkSchedules
                .AsNoTracking()
                .ToListAsync());

        Assert.Empty(
            await verificationContext
                .WorkScheduleDays
                .AsNoTracking()
                .ToListAsync());
    }

    [Fact]
    public async Task DeletingEmployeeWithHistoricalScheduleAssignment_IsRestricted()
    {
        await using SqliteConnection connection =
            await CreateOpenConnectionAsync();

        DbContextOptions<HrManagementDbContext> options =
            CreateOptions(
                connection);

        await EnsureCreatedAsync(
            options);

        Guid employeeId =
            Guid.NewGuid();

        Guid periodId =
            Guid.NewGuid();

        Guid scheduleId =
            Guid.NewGuid();

        await SeedEmployeePeriodScheduleAndAssignmentAsync(
            options,
            employeeId,
            periodId,
            scheduleId);

        await using var dbContext =
            new HrManagementDbContext(
                options);

        Employee employee =
            await dbContext
                .Employees
                .SingleAsync(
                    item =>
                        item.Id ==
                        employeeId);

        dbContext.Employees.Remove(
            employee);

        await Assert.ThrowsAsync<DbUpdateException>(
            () =>
                dbContext.SaveChangesAsync());
    }

    [Fact]
    public async Task DeletingScheduleWithHistoricalAssignment_IsRestricted()
    {
        await using SqliteConnection connection =
            await CreateOpenConnectionAsync();

        DbContextOptions<HrManagementDbContext> options =
            CreateOptions(
                connection);

        await EnsureCreatedAsync(
            options);

        Guid employeeId =
            Guid.NewGuid();

        Guid periodId =
            Guid.NewGuid();

        Guid scheduleId =
            Guid.NewGuid();

        await SeedEmployeePeriodScheduleAndAssignmentAsync(
            options,
            employeeId,
            periodId,
            scheduleId);

        await using var dbContext =
            new HrManagementDbContext(
                options);

        WorkSchedule schedule =
            await dbContext
                .WorkSchedules
                .SingleAsync(
                    item =>
                        item.Id ==
                        scheduleId);

        dbContext.WorkSchedules.Remove(
            schedule);

        await Assert.ThrowsAsync<DbUpdateException>(
            () =>
                dbContext.SaveChangesAsync());
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

    private static async Task AddEmployeeAsync(
        DbContextOptions<HrManagementDbContext> options,
        Guid employeeId)
    {
        await using var dbContext =
            new HrManagementDbContext(
                options);

        await dbContext.Employees.AddAsync(
            new Employee(
                employeeId,
                $"EMP-{employeeId:N}"[..20],
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

        await dbContext.SaveChangesAsync();
    }

    private static async Task
        SeedEmployeePeriodAndScheduleAsync(
            DbContextOptions<HrManagementDbContext> options,
            Guid employeeId,
            Guid periodId,
            Guid scheduleId)
    {
        await AddEmployeeAsync(
            options,
            employeeId);

        await using var dbContext =
            new HrManagementDbContext(
                options);

        await dbContext.EmploymentPeriods.AddAsync(
            new EmploymentPeriod(
                periodId,
                employeeId,
                new DateOnly(
                    2026,
                    1,
                    1)));

        await dbContext.WorkSchedules.AddAsync(
            new WorkSchedule(
                scheduleId,
                "OFFICE",
                "Văn phòng",
                "SE Asia Standard Time"));

        await dbContext.SaveChangesAsync();
    }

    private static async Task
        SeedEmployeePeriodScheduleAndAssignmentAsync(
            DbContextOptions<HrManagementDbContext> options,
            Guid employeeId,
            Guid periodId,
            Guid scheduleId)
    {
        await SeedEmployeePeriodAndScheduleAsync(
            options,
            employeeId,
            periodId,
            scheduleId);

        await using var dbContext =
            new HrManagementDbContext(
                options);

        await dbContext
            .EmployeeWorkScheduleAssignments
            .AddAsync(
                new EmployeeWorkScheduleAssignment(
                    Guid.NewGuid(),
                    employeeId,
                    periodId,
                    scheduleId,
                    new DateOnly(
                        2026,
                        1,
                        1)));

        await dbContext.SaveChangesAsync();
    }
}
