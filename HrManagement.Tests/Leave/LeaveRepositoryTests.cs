using HrManagement.Domain.Employees;
using HrManagement.Domain.Leave.Requests;
using HrManagement.Domain.Leave.Types;
using HrManagement.Infrastructure.Leave.Requests;
using HrManagement.Infrastructure.Leave.Types;
using HrManagement.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace HrManagement.Tests.Leave;

public sealed class LeaveRepositoryTests
{
    [Fact]
    public async Task LeaveType_GetById_ReturnsEntity()
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

        var repository =
            new EfLeaveTypeRepository(
                new TestDbContextFactory(
                    options));

        LeaveType? result =
            await repository.GetByIdAsync(
                seed.LeaveTypeId);

        Assert.NotNull(
            result);

        Assert.Equal(
            seed.LeaveTypeId,
            result!.Id);

        Assert.Equal(
            "ANNUAL",
            result.Code);
    }

    [Fact]
    public async Task LeaveType_GetByEmptyId_ReturnsNull()
    {
        await using SqliteConnection connection =
            await CreateOpenConnectionAsync();

        DbContextOptions<HrManagementDbContext> options =
            CreateOptions(
                connection);

        await EnsureCreatedAsync(
            options);

        var repository =
            new EfLeaveTypeRepository(
                new TestDbContextFactory(
                    options));

        LeaveType? result =
            await repository.GetByIdAsync(
                Guid.Empty);

        Assert.Null(
            result);
    }

    [Fact]
    public async Task LeaveRequest_GetById_ReturnsEntity()
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
                seed,
                new DateOnly(
                    2026,
                    8,
                    20),
                new DateOnly(
                    2026,
                    8,
                    22));

        await SeedRequestsAsync(
            options,
            request);

        var repository =
            new EfLeaveRequestRepository(
                new TestDbContextFactory(
                    options));

        LeaveRequest? result =
            await repository.GetByIdAsync(
                request.Id);

        Assert.NotNull(
            result);

        Assert.Equal(
            request.Id,
            result!.Id);

        Assert.Equal(
            LeaveRequestStatus.Pending,
            result.Status);
    }

    [Fact]
    public async Task LeaveRequest_GetByEmptyId_ReturnsNull()
    {
        await using SqliteConnection connection =
            await CreateOpenConnectionAsync();

        DbContextOptions<HrManagementDbContext> options =
            CreateOptions(
                connection);

        await EnsureCreatedAsync(
            options);

        var repository =
            new EfLeaveRequestRepository(
                new TestDbContextFactory(
                    options));

        LeaveRequest? result =
            await repository.GetByIdAsync(
                Guid.Empty);

        Assert.Null(
            result);
    }

    [Fact]
    public async Task OverlapQuery_ReturnsOnlyInclusiveOverlapsForEmployee()
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

        LeaveRequest before =
            CreateRequest(
                seed,
                new DateOnly(
                    2026,
                    8,
                    10),
                new DateOnly(
                    2026,
                    8,
                    12));

        LeaveRequest touchingStart =
            CreateRequest(
                seed,
                new DateOnly(
                    2026,
                    8,
                    19),
                new DateOnly(
                    2026,
                    8,
                    20));

        LeaveRequest inside =
            CreateRequest(
                seed,
                new DateOnly(
                    2026,
                    8,
                    21),
                new DateOnly(
                    2026,
                    8,
                    22));

        LeaveRequest touchingEnd =
            CreateRequest(
                seed,
                new DateOnly(
                    2026,
                    8,
                    24),
                new DateOnly(
                    2026,
                    8,
                    26));

        LeaveRequest after =
            CreateRequest(
                seed,
                new DateOnly(
                    2026,
                    8,
                    27),
                new DateOnly(
                    2026,
                    8,
                    28));

        SeedContext otherEmployee =
            await SeedContextAsync(
                options,
                leaveTypeCode: "OTHER");

        LeaveRequest otherEmployeeOverlap =
            CreateRequest(
                otherEmployee,
                new DateOnly(
                    2026,
                    8,
                    20),
                new DateOnly(
                    2026,
                    8,
                    24));

        await SeedRequestsAsync(
            options,
            before,
            touchingStart,
            inside,
            touchingEnd,
            after,
            otherEmployeeOverlap);

        var repository =
            new EfLeaveRequestRepository(
                new TestDbContextFactory(
                    options));

        IReadOnlyList<LeaveRequest> result =
            await repository
                .GetOverlappingByEmployeeAsync(
                    seed.EmployeeId,
                    new DateOnly(
                        2026,
                        8,
                        20),
                    new DateOnly(
                        2026,
                        8,
                        24));

        Assert.Equal(
            3,
            result.Count);

        Assert.Equal(
            [
                touchingStart.Id,
                inside.Id,
                touchingEnd.Id
            ],
            result
                .Select(
                    request =>
                        request.Id)
                .ToArray());
    }

    [Fact]
    public async Task InvalidOverlapQuery_ReturnsEmpty()
    {
        await using SqliteConnection connection =
            await CreateOpenConnectionAsync();

        DbContextOptions<HrManagementDbContext> options =
            CreateOptions(
                connection);

        await EnsureCreatedAsync(
            options);

        var repository =
            new EfLeaveRequestRepository(
                new TestDbContextFactory(
                    options));

        IReadOnlyList<LeaveRequest> emptyEmployee =
            await repository
                .GetOverlappingByEmployeeAsync(
                    Guid.Empty,
                    new DateOnly(
                        2026,
                        8,
                        20),
                    new DateOnly(
                        2026,
                        8,
                        21));

        IReadOnlyList<LeaveRequest> reversedRange =
            await repository
                .GetOverlappingByEmployeeAsync(
                    Guid.NewGuid(),
                    new DateOnly(
                        2026,
                        8,
                        21),
                    new DateOnly(
                        2026,
                        8,
                        20));

        Assert.Empty(
            emptyEmployee);

        Assert.Empty(
            reversedRange);
    }

    private static LeaveRequest CreateRequest(
        SeedContext seed,
        DateOnly startDate,
        DateOnly endDate)
    {
        return new LeaveRequest(
            Guid.NewGuid(),
            seed.EmployeeId,
            seed.EmploymentPeriodId,
            seed.LeaveTypeId,
            startDate,
            endDate,
            null,
            Utc(
                2026,
                8,
                1,
                3,
                0));
    }

    private static async Task SeedRequestsAsync(
        DbContextOptions<HrManagementDbContext> options,
        params LeaveRequest[] requests)
    {
        await using var dbContext =
            new HrManagementDbContext(
                options);

        dbContext.LeaveRequests.AddRange(
            requests);

        await dbContext.SaveChangesAsync();
    }

    private static async Task<SeedContext> SeedContextAsync(
        DbContextOptions<HrManagementDbContext> options,
        string leaveTypeCode = "ANNUAL")
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
                leaveTypeCode,
                $"Loại nghỉ {leaveTypeCode}",
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
