using HrManagement.Application.Workspaces.Overtime;
using HrManagement.Domain.Employees;
using HrManagement.Domain.Overtime.Requests;
using HrManagement.Infrastructure.Persistence;
using HrManagement.Infrastructure.Workspaces.Overtime;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace HrManagement.Tests.Overtime;

public sealed class OvertimeWorkspaceQueryServiceTests
{
    [Fact]
    public async Task GetAsync_ReturnsRequestsInSelectedMonthWithEmployeeIdentity()
    {
        await using TestDatabase database =
            await TestDatabase.CreateAsync();

        SeedResult seed =
            await database.SeedAsync();

        OvertimeWorkspaceSnapshot result =
            await database.Service.GetAsync(
                new OvertimeWorkspaceQuery(
                    2026,
                    8));

        Assert.Equal(
            2,
            result.Requests.Count);

        OvertimeWorkspaceItem first =
            result.Requests[0];

        Assert.Equal(
            seed.EmployeeId,
            first.EmployeeId);

        Assert.Equal(
            "EMP001",
            first.EmployeeCode);

        Assert.Equal(
            "Nguyễn Văn An",
            first.EmployeeName);

        Assert.Equal(
            new DateOnly(
                2026,
                8,
                28),
            first.WorkDate);
    }

    [Fact]
    public async Task GetAsync_WhenFilteringByEmployeeAndStatus_ReturnsMatchingRows()
    {
        await using TestDatabase database =
            await TestDatabase.CreateAsync();

        SeedResult seed =
            await database.SeedAsync();

        OvertimeWorkspaceSnapshot result =
            await database.Service.GetAsync(
                new OvertimeWorkspaceQuery(
                    2026,
                    8,
                    seed.EmployeeId,
                    OvertimeRequestStatus.Approved));

        OvertimeWorkspaceItem item =
            Assert.Single(
                result.Requests);

        Assert.Equal(
            OvertimeRequestStatus.Approved,
            item.Status);

        Assert.Equal(
            90,
            item.ApprovedMinutes);
    }

    [Fact]
    public async Task GetEmployeesAsync_ReturnsEmployeeOptionsOrderedByCode()
    {
        await using TestDatabase database =
            await TestDatabase.CreateAsync();

        await database.SeedAsync();

        IReadOnlyList<OvertimeEmployeeOption> employees =
            await database.Service
                .GetEmployeesAsync();

        OvertimeEmployeeOption employee =
            Assert.Single(
                employees);

        Assert.Equal(
            "EMP001",
            employee.EmployeeCode);

        Assert.Equal(
            "Nguyễn Văn An",
            employee.EmployeeName);
    }

    [Fact]
    public async Task GetHistoryAsync_ReturnsNewestTransitionFirst()
    {
        await using TestDatabase database =
            await TestDatabase.CreateAsync();

        SeedResult seed =
            await database.SeedAsync();

        IReadOnlyList<OvertimeStatusHistoryItem> history =
            await database.Service
                .GetHistoryAsync(
                    seed.ApprovedRequestId);

        OvertimeStatusHistoryItem item =
            Assert.Single(
                history);

        Assert.Equal(
            OvertimeRequestStatus.Pending,
            item.PreviousStatus);

        Assert.Equal(
            OvertimeRequestStatus.Approved,
            item.NewStatus);

        Assert.Equal(
            90,
            item.ApprovedMinutes);

        Assert.Equal(
            "admin",
            item.ChangedByUsername);

        Assert.Equal(
            "Duyệt một phần",
            item.Note);
    }

    [Fact]
    public async Task GetAsync_WhenMonthIsInvalid_Throws()
    {
        await using TestDatabase database =
            await TestDatabase.CreateAsync();

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () =>
                database.Service.GetAsync(
                    new OvertimeWorkspaceQuery(
                        2026,
                        13)));
    }

    private sealed record SeedResult(
        Guid EmployeeId,
        Guid ApprovedRequestId);

    private sealed class TestDatabase
        : IAsyncDisposable
    {
        private readonly SqliteConnection
            _connection;

        private readonly DbContextOptions<HrManagementDbContext>
            _options;

        public EfOvertimeWorkspaceQueryService Service
        {
            get;
        }

        private TestDatabase(
            SqliteConnection connection,
            DbContextOptions<HrManagementDbContext> options)
        {
            _connection =
                connection;

            _options =
                options;

            var factory =
                new TestDbContextFactory(
                    options);

            Service =
                new EfOvertimeWorkspaceQueryService(
                    factory);
        }

        public static async Task<TestDatabase> CreateAsync()
        {
            var connection =
                new SqliteConnection(
                    "Data Source=:memory:;Foreign Keys=True");

            await connection.OpenAsync();

            DbContextOptions<HrManagementDbContext> options =
                new DbContextOptionsBuilder<HrManagementDbContext>()
                    .UseSqlite(
                        connection)
                    .Options;

            await using (
                var dbContext =
                    new HrManagementDbContext(
                        options))
            {
                await dbContext.Database
                    .EnsureCreatedAsync();
            }

            return new TestDatabase(
                connection,
                options);
        }

        public async Task<SeedResult> SeedAsync()
        {
            Guid employeeId =
                Guid.NewGuid();

            Guid employmentPeriodId =
                Guid.NewGuid();

            var employee =
                new Employee(
                    employeeId,
                    "EMP001",
                    "Nguyễn Văn An",
                    email: null,
                    phoneNumber: null,
                    dateOfBirth: null,
                    hireDate:
                        new DateOnly(
                            2026,
                            1,
                            1),
                    department:
                        "Phát triển",
                    position:
                        "Lập trình viên",
                    status:
                        EmployeeStatus.Active);

            var employmentPeriod =
                new EmploymentPeriod(
                    employmentPeriodId,
                    employeeId,
                    new DateOnly(
                        2026,
                        1,
                        1));

            var pendingRequest =
                new OvertimeRequest(
                    Guid.NewGuid(),
                    employeeId,
                    employmentPeriodId,
                    new DateOnly(
                        2026,
                        8,
                        28),
                    120,
                    "Hoàn thành phát hành",
                    Utc(
                        11));

            var approvedRequest =
                new OvertimeRequest(
                    Guid.NewGuid(),
                    employeeId,
                    employmentPeriodId,
                    new DateOnly(
                        2026,
                        8,
                        27),
                    120,
                    "Hỗ trợ triển khai",
                    Utc(
                        10));

            OvertimeRequestStatusChange approval =
                approvedRequest.TransitionTo(
                    Guid.NewGuid(),
                    OvertimeRequestStatus.Approved,
                    Utc(
                        12),
                    "user-1",
                    "admin",
                    approvedMinutes:
                        90,
                    note:
                        "Duyệt một phần");

            var outsideMonthRequest =
                new OvertimeRequest(
                    Guid.NewGuid(),
                    employeeId,
                    employmentPeriodId,
                    new DateOnly(
                        2026,
                        9,
                        1),
                    60,
                    "Ngoài tháng kiểm thử",
                    Utc(
                        13));

            await using var dbContext =
                new HrManagementDbContext(
                    _options);

            dbContext.Employees.Add(
                employee);

            dbContext.EmploymentPeriods.Add(
                employmentPeriod);

            dbContext.OvertimeRequests.AddRange(
                pendingRequest,
                approvedRequest,
                outsideMonthRequest);

            dbContext.OvertimeRequestStatusChanges.Add(
                approval);

            await dbContext.SaveChangesAsync();

            return new SeedResult(
                employeeId,
                approvedRequest.Id);
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

    private static DateTime Utc(
        int hour)
    {
        return new DateTime(
            2026,
            8,
            27,
            hour,
            0,
            0,
            DateTimeKind.Utc);
    }
}
