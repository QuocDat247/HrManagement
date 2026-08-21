using HrManagement.Domain.Employees;
using HrManagement.Domain.Leave.Requests;
using HrManagement.Domain.Leave.Types;
using HrManagement.Infrastructure.Leave.Requests;
using HrManagement.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace HrManagement.Tests.Leave;

public sealed class LeaveRequestStatusHistoryRepositoryTests
{
    [Fact]
    public async Task GetByLeaveRequestId_ReturnsHistoryInChronologicalOrder()
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

        LeaveRequest request =
            CreateRequest(
                seed);

        LeaveRequestStatusChange approval =
            request.TransitionTo(
                Guid.NewGuid(),
                LeaveRequestStatus.Approved,
                Utc(
                    2026,
                    8,
                    21,
                    5,
                    0),
                "user-001",
                "admin");

        LeaveRequestStatusChange cancellation =
            request.TransitionTo(
                Guid.NewGuid(),
                LeaveRequestStatus.Cancelled,
                Utc(
                    2026,
                    8,
                    21,
                    6,
                    0),
                "user-001",
                "admin",
                "Hủy theo yêu cầu");

        await PersistRequestAndHistoryAsync(
            options,
            request,
            approval,
            cancellation);

        var repository =
            new EfLeaveRequestStatusHistoryRepository(
                new TestDbContextFactory(
                    options));

        IReadOnlyList<LeaveRequestStatusChange> history =
            await repository
                .GetByLeaveRequestIdAsync(
                    request.Id);

        Assert.Equal(
            2,
            history.Count);

        Assert.Equal(
            approval.Id,
            history[0].Id);

        Assert.Equal(
            LeaveRequestStatus.Pending,
            history[0].FromStatus);

        Assert.Equal(
            LeaveRequestStatus.Approved,
            history[0].ToStatus);

        Assert.Equal(
            cancellation.Id,
            history[1].Id);

        Assert.Equal(
            LeaveRequestStatus.Approved,
            history[1].FromStatus);

        Assert.Equal(
            LeaveRequestStatus.Cancelled,
            history[1].ToStatus);
    }

    [Fact]
    public async Task GetByLeaveRequestId_ExcludesHistoryForOtherRequest()
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

        LeaveRequest firstRequest =
            CreateRequest(
                seed);

        LeaveRequest secondRequest =
            CreateRequest(
                seed);

        LeaveRequestStatusChange firstChange =
            firstRequest.TransitionTo(
                Guid.NewGuid(),
                LeaveRequestStatus.Approved,
                Utc(
                    2026,
                    8,
                    21,
                    5,
                    0),
                "user-001",
                "admin");

        LeaveRequestStatusChange secondChange =
            secondRequest.TransitionTo(
                Guid.NewGuid(),
                LeaveRequestStatus.Rejected,
                Utc(
                    2026,
                    8,
                    21,
                    5,
                    30),
                "user-002",
                "manager");

        await using (
            var dbContext =
                new HrManagementDbContext(
                    options))
        {
            await dbContext.LeaveRequests.AddRangeAsync(
                firstRequest,
                secondRequest);

            await dbContext
                .LeaveRequestStatusChanges
                .AddRangeAsync(
                    firstChange,
                    secondChange);

            await dbContext.SaveChangesAsync();
        }

        var repository =
            new EfLeaveRequestStatusHistoryRepository(
                new TestDbContextFactory(
                    options));

        IReadOnlyList<LeaveRequestStatusChange> history =
            await repository
                .GetByLeaveRequestIdAsync(
                    firstRequest.Id);

        LeaveRequestStatusChange result =
            Assert.Single(
                history);

        Assert.Equal(
            firstChange.Id,
            result.Id);

        Assert.Equal(
            firstRequest.Id,
            result.LeaveRequestId);
    }

    [Fact]
    public async Task EmptyLeaveRequestId_ReturnsEmpty()
    {
        await using SqliteConnection connection =
            await CreateOpenConnectionAsync();

        DbContextOptions<HrManagementDbContext> options =
            CreateOptions(
                connection);

        await EnsureCreatedAsync(
            options);

        var repository =
            new EfLeaveRequestStatusHistoryRepository(
                new TestDbContextFactory(
                    options));

        IReadOnlyList<LeaveRequestStatusChange> history =
            await repository
                .GetByLeaveRequestIdAsync(
                    Guid.Empty);

        Assert.Empty(
            history);
    }

    [Fact]
    public async Task MissingLeaveRequestHistory_ReturnsEmpty()
    {
        await using SqliteConnection connection =
            await CreateOpenConnectionAsync();

        DbContextOptions<HrManagementDbContext> options =
            CreateOptions(
                connection);

        await EnsureCreatedAsync(
            options);

        var repository =
            new EfLeaveRequestStatusHistoryRepository(
                new TestDbContextFactory(
                    options));

        IReadOnlyList<LeaveRequestStatusChange> history =
            await repository
                .GetByLeaveRequestIdAsync(
                    Guid.NewGuid());

        Assert.Empty(
            history);
    }

    private static async Task PersistRequestAndHistoryAsync(
        DbContextOptions<HrManagementDbContext> options,
        LeaveRequest request,
        params LeaveRequestStatusChange[] changes)
    {
        await using var dbContext =
            new HrManagementDbContext(
                options);

        await dbContext.LeaveRequests.AddAsync(
            request);

        await dbContext
            .LeaveRequestStatusChanges
            .AddRangeAsync(
                changes);

        await dbContext.SaveChangesAsync();
    }

    private static LeaveRequest CreateRequest(
        SeedContext seed)
    {
        return new LeaveRequest(
            Guid.NewGuid(),
            seed.EmployeeId,
            seed.EmploymentPeriodId,
            seed.LeaveTypeId,
            new DateOnly(
                2026,
                8,
                25),
            new DateOnly(
                2026,
                8,
                26),
            null,
            Utc(
                2026,
                8,
                20,
                4,
                0));
    }

    private static async Task<SeedContext>
        SeedContextAsync(
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
                $"EMP{employeeId:N}"[..20],
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

    private static DateTime Utc(
        int year,
        int month,
        int day,
        int hour,
        int minute)
    {
        return new DateTime(
            year,
            month,
            day,
            hour,
            minute,
            0,
            DateTimeKind.Utc);
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
