using HrManagement.Domain.Attendance.Schedules;
using HrManagement.Domain.Employees;
using HrManagement.Infrastructure.Attendance.Schedules;
using HrManagement.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace HrManagement.Tests.Attendance;

public sealed class EmployeeWorkScheduleAssignmentPersistenceTests
{
    [Fact]
    public async Task WorkScheduleRepository_WhenScheduleExists_ReturnsSchedule()
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

        await AddScheduleAsync(
            options,
            scheduleId,
            "OFFICE");

        var repository =
            new EfWorkScheduleRepository(
                new TestDbContextFactory(
                    options));

        WorkSchedule? result =
            await repository.GetByIdAsync(
                scheduleId);

        Assert.NotNull(
            result);

        Assert.Equal(
            scheduleId,
            result!.Id);

        Assert.Equal(
            "OFFICE",
            result.Code);
    }

    [Fact]
    public async Task WorkScheduleRepository_WhenScheduleDoesNotExist_ReturnsNull()
    {
        await using SqliteConnection connection =
            await CreateOpenConnectionAsync();

        DbContextOptions<HrManagementDbContext> options =
            CreateOptions(
                connection);

        await EnsureCreatedAsync(
            options);

        var repository =
            new EfWorkScheduleRepository(
                new TestDbContextFactory(
                    options));

        WorkSchedule? result =
            await repository.GetByIdAsync(
                Guid.NewGuid());

        Assert.Null(
            result);
    }

    [Fact]
    public async Task AssignmentRepository_ReturnsAssignmentsInTimelineOrder()
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

        await SeedEmployeePeriodAndSchedulesAsync(
            options,
            employeeId,
            periodId,
            firstScheduleId,
            secondScheduleId);

        Guid firstAssignmentId =
            Guid.NewGuid();

        Guid secondAssignmentId =
            Guid.NewGuid();

        await using (
            var dbContext =
                new HrManagementDbContext(
                    options))
        {
            await dbContext
                .EmployeeWorkScheduleAssignments
                .AddRangeAsync(
                    new EmployeeWorkScheduleAssignment(
                        secondAssignmentId,
                        employeeId,
                        periodId,
                        secondScheduleId,
                        new DateOnly(
                            2026,
                            9,
                            1)),
                    new EmployeeWorkScheduleAssignment(
                        firstAssignmentId,
                        employeeId,
                        periodId,
                        firstScheduleId,
                        new DateOnly(
                            2026,
                            1,
                            1),
                        new DateOnly(
                            2026,
                            8,
                            31)));

            await dbContext.SaveChangesAsync();
        }

        var repository =
            new EfEmployeeWorkScheduleAssignmentRepository(
                new TestDbContextFactory(
                    options));

        IReadOnlyList<EmployeeWorkScheduleAssignment> result =
            await repository.GetByEmployeeIdAsync(
                employeeId);

        Assert.Equal(
            2,
            result.Count);

        Assert.Equal(
            firstAssignmentId,
            result[0].Id);

        Assert.Equal(
            secondAssignmentId,
            result[1].Id);
    }

    [Fact]
    public async Task ApplyAsync_WhenInitialAssignment_AddsOpenAssignment()
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

        await SeedEmployeePeriodAndScheduleAsync(
            options,
            employeeId,
            periodId,
            scheduleId);

        Guid assignmentId =
            Guid.NewGuid();

        var persistence =
            CreatePersistence(
                options);

        await persistence.ApplyAsync(
            null,
            new EmployeeWorkScheduleAssignment(
                assignmentId,
                employeeId,
                periodId,
                scheduleId,
                new DateOnly(
                    2026,
                    8,
                    1)));

        await using var dbContext =
            new HrManagementDbContext(
                options);

        EmployeeWorkScheduleAssignment saved =
            await dbContext
                .EmployeeWorkScheduleAssignments
                .AsNoTracking()
                .SingleAsync();

        Assert.Equal(
            assignmentId,
            saved.Id);

        Assert.True(
            saved.IsOpen);
    }

    [Fact]
    public async Task ApplyAsync_WhenChangingSchedule_ClosesOldAndAddsNew()
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

        Guid oldScheduleId =
            Guid.NewGuid();

        Guid newScheduleId =
            Guid.NewGuid();

        await SeedEmployeePeriodAndSchedulesAsync(
            options,
            employeeId,
            periodId,
            oldScheduleId,
            newScheduleId);

        Guid oldAssignmentId =
            Guid.NewGuid();

        await AddAssignmentAsync(
            options,
            new EmployeeWorkScheduleAssignment(
                oldAssignmentId,
                employeeId,
                periodId,
                oldScheduleId,
                new DateOnly(
                    2026,
                    1,
                    1)));

        var closedAssignment =
            new EmployeeWorkScheduleAssignment(
                oldAssignmentId,
                employeeId,
                periodId,
                oldScheduleId,
                new DateOnly(
                    2026,
                    1,
                    1),
                new DateOnly(
                    2026,
                    8,
                    31));

        Guid newAssignmentId =
            Guid.NewGuid();

        var newAssignment =
            new EmployeeWorkScheduleAssignment(
                newAssignmentId,
                employeeId,
                periodId,
                newScheduleId,
                new DateOnly(
                    2026,
                    9,
                    1));

        var persistence =
            CreatePersistence(
                options);

        await persistence.ApplyAsync(
            closedAssignment,
            newAssignment);

        await using var dbContext =
            new HrManagementDbContext(
                options);

        List<EmployeeWorkScheduleAssignment> assignments =
            await dbContext
                .EmployeeWorkScheduleAssignments
                .AsNoTracking()
                .OrderBy(
                    assignment =>
                        assignment.EffectiveFrom)
                .ToListAsync();

        Assert.Equal(
            2,
            assignments.Count);

        Assert.Equal(
            oldAssignmentId,
            assignments[0].Id);

        Assert.Equal(
            new DateOnly(
                2026,
                8,
                31),
            assignments[0].EffectiveTo);

        Assert.Equal(
            newAssignmentId,
            assignments[1].Id);

        Assert.True(
            assignments[1].IsOpen);
    }

    [Fact]
    public async Task ApplyAsync_WhenPersistedAssignmentChanged_ThrowsConcurrencyException()
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

        Guid oldScheduleId =
            Guid.NewGuid();

        Guid newScheduleId =
            Guid.NewGuid();

        await SeedEmployeePeriodAndSchedulesAsync(
            options,
            employeeId,
            periodId,
            oldScheduleId,
            newScheduleId);

        Guid oldAssignmentId =
            Guid.NewGuid();

        await AddAssignmentAsync(
            options,
            new EmployeeWorkScheduleAssignment(
                oldAssignmentId,
                employeeId,
                periodId,
                oldScheduleId,
                new DateOnly(
                    2026,
                    1,
                    1),
                new DateOnly(
                    2026,
                    7,
                    31)));

        var requestedClosed =
            new EmployeeWorkScheduleAssignment(
                oldAssignmentId,
                employeeId,
                periodId,
                oldScheduleId,
                new DateOnly(
                    2026,
                    1,
                    1),
                new DateOnly(
                    2026,
                    8,
                    31));

        var requestedNew =
            new EmployeeWorkScheduleAssignment(
                Guid.NewGuid(),
                employeeId,
                periodId,
                newScheduleId,
                new DateOnly(
                    2026,
                    9,
                    1));

        var persistence =
            CreatePersistence(
                options);

        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(
            () =>
                persistence.ApplyAsync(
                    requestedClosed,
                    requestedNew));
    }

    [Fact]
    public async Task ApplyAsync_WhenNewInsertFails_RollsBackOldAssignmentClosure()
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

        Guid oldScheduleId =
            Guid.NewGuid();

        Guid newScheduleId =
            Guid.NewGuid();

        await SeedEmployeePeriodAndSchedulesAsync(
            options,
            employeeId,
            periodId,
            oldScheduleId,
            newScheduleId);

        Guid duplicateAssignmentId =
            Guid.NewGuid();

        await AddAssignmentAsync(
            options,
            new EmployeeWorkScheduleAssignment(
                duplicateAssignmentId,
                employeeId,
                periodId,
                oldScheduleId,
                new DateOnly(
                    2026,
                    1,
                    1),
                new DateOnly(
                    2026,
                    6,
                    30)));

        Guid currentAssignmentId =
            Guid.NewGuid();

        await AddAssignmentAsync(
            options,
            new EmployeeWorkScheduleAssignment(
                currentAssignmentId,
                employeeId,
                periodId,
                oldScheduleId,
                new DateOnly(
                    2026,
                    7,
                    1)));

        var closedAssignment =
            new EmployeeWorkScheduleAssignment(
                currentAssignmentId,
                employeeId,
                periodId,
                oldScheduleId,
                new DateOnly(
                    2026,
                    7,
                    1),
                new DateOnly(
                    2026,
                    8,
                    31));

        var failingNewAssignment =
            new EmployeeWorkScheduleAssignment(
                duplicateAssignmentId,
                employeeId,
                periodId,
                newScheduleId,
                new DateOnly(
                    2026,
                    9,
                    1));

        var persistence =
            CreatePersistence(
                options);

        await Assert.ThrowsAsync<DbUpdateException>(
            () =>
                persistence.ApplyAsync(
                    closedAssignment,
                    failingNewAssignment));

        await using var dbContext =
            new HrManagementDbContext(
                options);

        EmployeeWorkScheduleAssignment current =
            await dbContext
                .EmployeeWorkScheduleAssignments
                .AsNoTracking()
                .SingleAsync(
                    assignment =>
                        assignment.Id ==
                        currentAssignmentId);

        Assert.True(
            current.IsOpen);

        Assert.Null(
            current.EffectiveTo);
    }

    private static EfEmployeeWorkScheduleAssignmentPersistence
        CreatePersistence(
            DbContextOptions<HrManagementDbContext> options)
    {
        return new EfEmployeeWorkScheduleAssignmentPersistence(
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

    private static async Task AddScheduleAsync(
        DbContextOptions<HrManagementDbContext> options,
        Guid scheduleId,
        string code)
    {
        await using var dbContext =
            new HrManagementDbContext(
                options);

        await dbContext.WorkSchedules.AddAsync(
            new WorkSchedule(
                scheduleId,
                code,
                $"Lịch {code}",
                "SE Asia Standard Time"));

        await dbContext.SaveChangesAsync();
    }

    private static async Task SeedEmployeePeriodAndScheduleAsync(
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
                "Lịch OFFICE",
                "SE Asia Standard Time"));

        await dbContext.SaveChangesAsync();
    }

    private static async Task SeedEmployeePeriodAndSchedulesAsync(
        DbContextOptions<HrManagementDbContext> options,
        Guid employeeId,
        Guid periodId,
        Guid firstScheduleId,
        Guid secondScheduleId)
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

        await dbContext.WorkSchedules.AddRangeAsync(
            new WorkSchedule(
                firstScheduleId,
                "OLD",
                "Lịch OLD",
                "SE Asia Standard Time"),
            new WorkSchedule(
                secondScheduleId,
                "NEW",
                "Lịch NEW",
                "SE Asia Standard Time"));

        await dbContext.SaveChangesAsync();
    }

    private static async Task AddAssignmentAsync(
        DbContextOptions<HrManagementDbContext> options,
        EmployeeWorkScheduleAssignment assignment)
    {
        await using var dbContext =
            new HrManagementDbContext(
                options);

        await dbContext
            .EmployeeWorkScheduleAssignments
            .AddAsync(
                assignment);

        await dbContext.SaveChangesAsync();
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
