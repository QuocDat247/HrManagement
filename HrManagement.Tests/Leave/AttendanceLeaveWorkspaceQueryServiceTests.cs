using HrManagement.Application.Workspaces.AttendanceLeave;
using HrManagement.Domain.Attendance.Calculations;
using HrManagement.Domain.Attendance.Records;
using HrManagement.Domain.Attendance.Schedules;
using HrManagement.Domain.Employees;
using HrManagement.Domain.Leave.Requests;
using HrManagement.Domain.Leave.Types;
using HrManagement.Infrastructure.Persistence;
using HrManagement.Infrastructure.Workspaces.AttendanceLeave;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace HrManagement.Tests.Leave;

public sealed class AttendanceLeaveWorkspaceQueryServiceTests
{
    [Fact]
    public async Task GetAsync_ReturnsAttendanceProjectionWithEmployeeIdentity()
    {
        await using SqliteConnection connection =
            await CreateOpenConnectionAsync();

        DbContextOptions<HrManagementDbContext> options =
            CreateOptions(
                connection);

        await EnsureCreatedAsync(
            options);

        SeedContext seed =
            await SeedEmployeeAsync(
                options,
                "A");

        Guid attendanceId =
            await AddAttendanceAsync(
                options,
                seed,
                new DateOnly(
                    2026,
                    8,
                    21));

        var service =
            CreateService(
                options);

        AttendanceLeaveWorkspaceSnapshot result =
            await service.GetAsync(
                new AttendanceLeaveWorkspaceQuery(
                    new DateOnly(
                        2026,
                        8,
                        20),
                    new DateOnly(
                        2026,
                        8,
                        22)));

        AttendanceWorkspaceItem attendance =
            Assert.Single(
                result.Attendance);

        Assert.Equal(
            attendanceId,
            attendance.AttendanceRecordId);

        Assert.Equal(
            seed.EmployeeId,
            attendance.EmployeeId);

        Assert.Equal(
            "EMP-A",
            attendance.EmployeeCode);

        Assert.Equal(
            "Nhân viên A",
            attendance.EmployeeName);

        Assert.Equal(
            new DateOnly(
                2026,
                8,
                21),
            attendance.WorkDate);

        Assert.True(
            attendance.IsWorkingDay);

        Assert.Equal(
            new TimeOnly(
                8,
                0),
            attendance.ExpectedStartTime);

        Assert.Equal(
            new TimeOnly(
                17,
                0),
            attendance.ExpectedEndTime);

        Assert.Equal(
            AttendanceCalculationStatus.NotCalculated,
            attendance.Status);
    }

    [Fact]
    public async Task GetAsync_ReturnsLeaveProjectionWithTypeAndStatus()
    {
        await using SqliteConnection connection =
            await CreateOpenConnectionAsync();

        DbContextOptions<HrManagementDbContext> options =
            CreateOptions(
                connection);

        await EnsureCreatedAsync(
            options);

        SeedContext seed =
            await SeedEmployeeAsync(
                options,
                "A");

        Guid leaveRequestId =
            await AddLeaveAsync(
                options,
                seed,
                new DateOnly(
                    2026,
                    8,
                    25),
                new DateOnly(
                    2026,
                    8,
                    26),
                LeaveRequestStatus.Approved);

        var service =
            CreateService(
                options);

        AttendanceLeaveWorkspaceSnapshot result =
            await service.GetAsync(
                new AttendanceLeaveWorkspaceQuery(
                    new DateOnly(
                        2026,
                        8,
                        25),
                    new DateOnly(
                        2026,
                        8,
                        26)));

        LeaveWorkspaceItem leave =
            Assert.Single(
                result.LeaveRequests);

        Assert.Equal(
            leaveRequestId,
            leave.LeaveRequestId);

        Assert.Equal(
            "EMP-A",
            leave.EmployeeCode);

        Assert.Equal(
            "Nhân viên A",
            leave.EmployeeName);

        Assert.Equal(
            "LEAVE-A",
            leave.LeaveTypeCode);

        Assert.Equal(
            "Loại nghỉ A",
            leave.LeaveTypeName);

        Assert.True(
            leave.IsPaid);

        Assert.Equal(
            LeaveRequestStatus.Approved,
            leave.Status);

        Assert.Equal(
            DateTimeKind.Utc,
            leave.SubmittedAtUtc.Kind);

        Assert.Equal(
            "Kiểm thử workspace",
            leave.Reason);
    }

    [Fact]
    public async Task GetAsync_FiltersAttendanceAndUsesLeaveRangeIntersection()
    {
        await using SqliteConnection connection =
            await CreateOpenConnectionAsync();

        DbContextOptions<HrManagementDbContext> options =
            CreateOptions(
                connection);

        await EnsureCreatedAsync(
            options);

        SeedContext seed =
            await SeedEmployeeAsync(
                options,
                "A");

        Guid includedAttendance =
            await AddAttendanceAsync(
                options,
                seed,
                new DateOnly(
                    2026,
                    8,
                    21));

        await AddAttendanceAsync(
            options,
            seed,
            new DateOnly(
                2026,
                8,
                23));

        Guid overlappingLeave =
            await AddLeaveAsync(
                options,
                seed,
                new DateOnly(
                    2026,
                    8,
                    20),
                new DateOnly(
                    2026,
                    8,
                    22));

        await AddLeaveAsync(
            options,
            seed,
            new DateOnly(
                2026,
                8,
                23),
            new DateOnly(
                2026,
                8,
                24));

        var service =
            CreateService(
                options);

        AttendanceLeaveWorkspaceSnapshot result =
            await service.GetAsync(
                new AttendanceLeaveWorkspaceQuery(
                    new DateOnly(
                        2026,
                        8,
                        21),
                    new DateOnly(
                        2026,
                        8,
                        21)));

        AttendanceWorkspaceItem attendance =
            Assert.Single(
                result.Attendance);

        Assert.Equal(
            includedAttendance,
            attendance.AttendanceRecordId);

        LeaveWorkspaceItem leave =
            Assert.Single(
                result.LeaveRequests);

        Assert.Equal(
            overlappingLeave,
            leave.LeaveRequestId);
    }

    [Fact]
    public async Task GetAsync_EmployeeFilterAppliesToBothCollections()
    {
        await using SqliteConnection connection =
            await CreateOpenConnectionAsync();

        DbContextOptions<HrManagementDbContext> options =
            CreateOptions(
                connection);

        await EnsureCreatedAsync(
            options);

        SeedContext first =
            await SeedEmployeeAsync(
                options,
                "A");

        SeedContext second =
            await SeedEmployeeAsync(
                options,
                "B");

        await AddAttendanceAsync(
            options,
            first,
            new DateOnly(
                2026,
                8,
                21));

        await AddAttendanceAsync(
            options,
            second,
            new DateOnly(
                2026,
                8,
                21));

        await AddLeaveAsync(
            options,
            first,
            new DateOnly(
                2026,
                8,
                22),
            new DateOnly(
                2026,
                8,
                22));

        await AddLeaveAsync(
            options,
            second,
            new DateOnly(
                2026,
                8,
                22),
            new DateOnly(
                2026,
                8,
                22));

        var service =
            CreateService(
                options);

        AttendanceLeaveWorkspaceSnapshot result =
            await service.GetAsync(
                new AttendanceLeaveWorkspaceQuery(
                    new DateOnly(
                        2026,
                        8,
                        20),
                    new DateOnly(
                        2026,
                        8,
                        23),
                    first.EmployeeId));

        AttendanceWorkspaceItem attendance =
            Assert.Single(
                result.Attendance);

        LeaveWorkspaceItem leave =
            Assert.Single(
                result.LeaveRequests);

        Assert.Equal(
            first.EmployeeId,
            attendance.EmployeeId);

        Assert.Equal(
            first.EmployeeId,
            leave.EmployeeId);
    }

    [Fact]
    public async Task GetAsync_InvalidDateRangeThrows()
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

        await Assert.ThrowsAsync<ArgumentException>(
            () =>
                service.GetAsync(
                    new AttendanceLeaveWorkspaceQuery(
                        new DateOnly(
                            2026,
                            8,
                            22),
                        new DateOnly(
                            2026,
                            8,
                            21))));
    }

    [Fact]
    public async Task GetEmployeesAsync_ReturnsAllEmployeesOrderedByCode()
    {
        await using SqliteConnection connection =
            await CreateOpenConnectionAsync();

        DbContextOptions<HrManagementDbContext> options =
            CreateOptions(
                connection);

        await EnsureCreatedAsync(
            options);

        await SeedEmployeeAsync(
            options,
            "B");

        await SeedEmployeeAsync(
            options,
            "A");

        IAttendanceLeaveWorkspaceQueryService service =
            CreateService(
                options);

        IReadOnlyList<AttendanceLeaveEmployeeItem> employees =
            await service.GetEmployeesAsync();

        Assert.Equal(
            2,
            employees.Count);

        Assert.Equal(
            "EMP-A",
            employees[0].EmployeeCode);

        Assert.Equal(
            "EMP-B",
            employees[1].EmployeeCode);
    }

    [Fact]
    public async Task GetActiveLeaveTypesAsync_ReturnsOnlyActiveTypes()
    {
        await using SqliteConnection connection =
            await CreateOpenConnectionAsync();

        DbContextOptions<HrManagementDbContext> options =
            CreateOptions(
                connection);

        await EnsureCreatedAsync(
            options);

        await using (
            var dbContext =
                new HrManagementDbContext(
                    options))
        {
            await dbContext.LeaveTypes.AddRangeAsync(
                new LeaveType(
                    Guid.NewGuid(),
                    "ACTIVE",
                    "Loại đang dùng",
                    isPaid: true,
                    isActive: true),

                new LeaveType(
                    Guid.NewGuid(),
                    "INACTIVE",
                    "Loại ngừng dùng",
                    isPaid: false,
                    isActive: false));

            await dbContext.SaveChangesAsync();
        }

        IAttendanceLeaveWorkspaceQueryService service =
            CreateService(
                options);

        IReadOnlyList<LeaveTypeWorkspaceOption> result =
            await service.GetActiveLeaveTypesAsync();

        LeaveTypeWorkspaceOption item =
            Assert.Single(
                result);

        Assert.Equal(
            "ACTIVE",
            item.Code);

        Assert.Equal(
            "Loại đang dùng",
            item.Name);

        Assert.True(
            item.IsPaid);
    }

    private static IAttendanceLeaveWorkspaceQueryService
        CreateService(
            DbContextOptions<HrManagementDbContext> options)
    {
        return new EfAttendanceLeaveWorkspaceQueryService(
            new TestDbContextFactory(
                options));
    }

    private static async Task<SeedContext>
        SeedEmployeeAsync(
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

        Guid leaveTypeId =
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
                "Asia/Ho_Chi_Minh"));

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

        await dbContext.LeaveTypes.AddAsync(
            new LeaveType(
                leaveTypeId,
                $"LEAVE-{suffix}",
                $"Loại nghỉ {suffix}",
                isPaid: true));

        await dbContext.SaveChangesAsync();

        return new SeedContext(
            employeeId,
            employmentPeriodId,
            scheduleId,
            assignmentId,
            leaveTypeId);
    }

    private static async Task<Guid> AddAttendanceAsync(
        DbContextOptions<HrManagementDbContext> options,
        SeedContext seed,
        DateOnly workDate)
    {
        Guid attendanceId =
            Guid.NewGuid();

        await using var dbContext =
            new HrManagementDbContext(
                options);

        await dbContext.AttendanceRecords.AddAsync(
            new AttendanceRecord(
                attendanceId,
                seed.EmployeeId,
                seed.EmploymentPeriodId,
                seed.ScheduleAssignmentId,
                seed.ScheduleId,
                workDate,
                "Asia/Ho_Chi_Minh",
                isWorkingDay: true,
                expectedStartTime:
                    new TimeOnly(
                        8,
                        0),
                expectedEndTime:
                    new TimeOnly(
                        17,
                        0),
                expectedBreakMinutes:
                    60));

        await dbContext.SaveChangesAsync();

        return attendanceId;
    }

    private static async Task<Guid> AddLeaveAsync(
        DbContextOptions<HrManagementDbContext> options,
        SeedContext seed,
        DateOnly startDate,
        DateOnly endDate,
        LeaveRequestStatus status =
            LeaveRequestStatus.Pending)
    {
        Guid leaveRequestId =
            Guid.NewGuid();

        var request =
            new LeaveRequest(
                leaveRequestId,
                seed.EmployeeId,
                seed.EmploymentPeriodId,
                seed.LeaveTypeId,
                startDate,
                endDate,
                "  Kiểm thử workspace  ",
                new DateTime(
                    2026,
                    8,
                    20,
                    4,
                    0,
                    0,
                    DateTimeKind.Utc));

        if (status !=
            LeaveRequestStatus.Pending)
        {
            request.TransitionTo(
                Guid.NewGuid(),
                status,
                new DateTime(
                    2026,
                    8,
                    20,
                    5,
                    0,
                    0,
                    DateTimeKind.Utc),
                "user-001",
                "admin");
        }

        await using var dbContext =
            new HrManagementDbContext(
                options);

        await dbContext.LeaveRequests.AddAsync(
            request);

        await dbContext.SaveChangesAsync();

        return leaveRequestId;
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

    private sealed record SeedContext(
        Guid EmployeeId,
        Guid EmploymentPeriodId,
        Guid ScheduleId,
        Guid ScheduleAssignmentId,
        Guid LeaveTypeId);

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
