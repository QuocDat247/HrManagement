using HrManagement.Application.Attendance.Calculations;
using HrManagement.Domain.Employees;
using HrManagement.Domain.Leave.Requests;
using HrManagement.Domain.Leave.Types;
using HrManagement.Infrastructure.Attendance.Calculations;
using HrManagement.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace HrManagement.Tests.Attendance;

public sealed class ApprovedLeaveAttendanceResolverTests
{
    [Fact]
    public async Task ApprovedLeaveCoveringWorkDate_ReturnsInput()
    {
        await using SqliteConnection connection =
            await CreateOpenConnectionAsync();

        DbContextOptions<HrManagementDbContext> options =
            CreateOptions(
                connection);

        await EnsureCreatedAsync(
            options);

        SeedContext seed =
            await SeedContextAsync(
                options);

        Guid requestId =
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
                    22),
                LeaveRequestStatus.Approved);

        IApprovedLeaveAttendanceResolver resolver =
            CreateResolver(
                options);

        ApprovedLeaveAttendanceInput? result =
            await resolver.ResolveAsync(
                seed.EmployeeId,
                seed.EmploymentPeriodId,
                new DateOnly(
                    2026,
                    8,
                    21));

        Assert.NotNull(
            result);

        Assert.Equal(
            requestId,
            result.LeaveRequestId);

        Assert.Equal(
            seed.LeaveTypeId,
            result.LeaveTypeId);
    }

    [Fact]
    public async Task PendingLeave_DoesNotResolve()
    {
        await using SqliteConnection connection =
            await CreateOpenConnectionAsync();

        DbContextOptions<HrManagementDbContext> options =
            CreateOptions(
                connection);

        await EnsureCreatedAsync(
            options);

        SeedContext seed =
            await SeedContextAsync(
                options);

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
                22),
            LeaveRequestStatus.Pending);

        IApprovedLeaveAttendanceResolver resolver =
            CreateResolver(
                options);

        ApprovedLeaveAttendanceInput? result =
            await resolver.ResolveAsync(
                seed.EmployeeId,
                seed.EmploymentPeriodId,
                new DateOnly(
                    2026,
                    8,
                    21));

        Assert.Null(
            result);
    }

    [Fact]
    public async Task RejectedAndCancelledLeave_DoNotResolve()
    {
        await using SqliteConnection connection =
            await CreateOpenConnectionAsync();

        DbContextOptions<HrManagementDbContext> options =
            CreateOptions(
                connection);

        await EnsureCreatedAsync(
            options);

        SeedContext seed =
            await SeedContextAsync(
                options);

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
                21),
            LeaveRequestStatus.Rejected);

        await AddLeaveAsync(
            options,
            seed,
            new DateOnly(
                2026,
                8,
                21),
            new DateOnly(
                2026,
                8,
                22),
            LeaveRequestStatus.Cancelled);

        IApprovedLeaveAttendanceResolver resolver =
            CreateResolver(
                options);

        ApprovedLeaveAttendanceInput? result =
            await resolver.ResolveAsync(
                seed.EmployeeId,
                seed.EmploymentPeriodId,
                new DateOnly(
                    2026,
                    8,
                    21));

        Assert.Null(
            result);
    }

    [Fact]
    public async Task ApprovedLeaveOutsideWorkDate_DoesNotResolve()
    {
        await using SqliteConnection connection =
            await CreateOpenConnectionAsync();

        DbContextOptions<HrManagementDbContext> options =
            CreateOptions(
                connection);

        await EnsureCreatedAsync(
            options);

        SeedContext seed =
            await SeedContextAsync(
                options);

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
                22),
            LeaveRequestStatus.Approved);

        IApprovedLeaveAttendanceResolver resolver =
            CreateResolver(
                options);

        ApprovedLeaveAttendanceInput? result =
            await resolver.ResolveAsync(
                seed.EmployeeId,
                seed.EmploymentPeriodId,
                new DateOnly(
                    2026,
                    8,
                    23));

        Assert.Null(
            result);
    }

    private static IApprovedLeaveAttendanceResolver CreateResolver(
        DbContextOptions<HrManagementDbContext> options)
    {
        return new EfApprovedLeaveAttendanceResolver(
            new TestDbContextFactory(
                options));
    }

    private static async Task<SeedContext> SeedContextAsync(
        DbContextOptions<HrManagementDbContext> options)
    {
        Guid employeeId =
            Guid.NewGuid();

        Guid employmentPeriodId =
            Guid.NewGuid();

        Guid leaveTypeId =
            Guid.NewGuid();

        await using var dbContext =
            new HrManagementDbContext(
                options);

        await dbContext.Employees.AddAsync(
            new Employee(
                employeeId,
                "EMP-LEAVE",
                "Nhân viên nghỉ phép",
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

        await dbContext.LeaveTypes.AddAsync(
            new LeaveType(
                leaveTypeId,
                "ANNUAL",
                "Nghỉ phép năm",
                isPaid: true));

        await dbContext.SaveChangesAsync();

        return new SeedContext(
            employeeId,
            employmentPeriodId,
            leaveTypeId);
    }

    private static async Task<Guid> AddLeaveAsync(
        DbContextOptions<HrManagementDbContext> options,
        SeedContext seed,
        DateOnly startDate,
        DateOnly endDate,
        LeaveRequestStatus status)
    {
        Guid requestId =
            Guid.NewGuid();

        var request =
            new LeaveRequest(
                requestId,
                seed.EmployeeId,
                seed.EmploymentPeriodId,
                seed.LeaveTypeId,
                startDate,
                endDate,
                null,
                new DateTime(
                    2026,
                    8,
                    19,
                    4,
                    0,
                    0,
                    DateTimeKind.Utc));

        LeaveRequestStatusChange? statusChange =
            null;

        if (status !=
            LeaveRequestStatus.Pending)
        {
            statusChange =
                request.TransitionTo(
                    Guid.NewGuid(),
                    status,
                    new DateTime(
                        2026,
                        8,
                        19,
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

        if (statusChange is not null)
        {
            await dbContext
                .LeaveRequestStatusChanges
                .AddAsync(
                    statusChange);
        }

        await dbContext.SaveChangesAsync();

        return requestId;
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
