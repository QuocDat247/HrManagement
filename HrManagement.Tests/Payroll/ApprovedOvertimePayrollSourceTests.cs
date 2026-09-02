using HrManagement.Application.Payroll.Calculations;
using HrManagement.Domain.Employees;
using HrManagement.Domain.Overtime.Requests;
using HrManagement.Infrastructure.Payroll.Calculations;
using HrManagement.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace HrManagement.Tests.Payroll;

public sealed class ApprovedOvertimePayrollSourceTests
{
    [Fact]
    public async Task GetApprovedAsync_ReturnsOnlyCurrentlyApprovedRequests()
    {
        await using TestDatabase database =
            await TestDatabase.CreateAsync();

        SeedResult seed =
            await database.SeedAsync();

        IReadOnlyList<ApprovedOvertimePayrollItem> rows =
            await database.Source.GetApprovedAsync(
                [seed.EmployeeId],
                new DateOnly(
                    2026,
                    8,
                    1),
                new DateOnly(
                    2026,
                    8,
                    31));

        ApprovedOvertimePayrollItem item =
            Assert.Single(
                rows);

        Assert.Equal(
            seed.ApprovedRequestId,
            item.OvertimeRequestId);

        Assert.Equal(
            seed.EmployeeId,
            item.EmployeeId);

        Assert.Equal(
            new DateOnly(
                2026,
                8,
                11),
            item.WorkDate);

        Assert.Equal(
            90,
            item.ApprovedMinutes);
    }

    [Fact]
    public async Task GetApprovedAsync_WhenEmployeeListIsEmpty_ReturnsEmpty()
    {
        await using TestDatabase database =
            await TestDatabase.CreateAsync();

        IReadOnlyList<ApprovedOvertimePayrollItem> rows =
            await database.Source.GetApprovedAsync(
                [],
                new DateOnly(
                    2026,
                    8,
                    1),
                new DateOnly(
                    2026,
                    8,
                    31));

        Assert.Empty(
            rows);
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

        public EfApprovedOvertimePayrollSource Source
        {
            get;
        }

        private TestDatabase(
            SqliteConnection connection,
            DbContextOptions<HrManagementDbContext> options,
            EfApprovedOvertimePayrollSource source)
        {
            _connection =
                connection;

            _options =
                options;

            Source =
                source;
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

            var factory =
                new TestDbContextFactory(
                    options);

            await using (
                HrManagementDbContext dbContext =
                    await factory.CreateDbContextAsync())
            {
                await dbContext.Database
                    .EnsureCreatedAsync();
            }

            return new TestDatabase(
                connection,
                options,
                new EfApprovedOvertimePayrollSource(
                    factory));
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
                    email:
                        null,
                    phoneNumber:
                        null,
                    dateOfBirth:
                        null,
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
                CreateRequest(
                    employeeId,
                    employmentPeriodId,
                    new DateOnly(
                        2026,
                        8,
                        10),
                    60);

            var approvedRequest =
                CreateRequest(
                    employeeId,
                    employmentPeriodId,
                    new DateOnly(
                        2026,
                        8,
                        11),
                    120);

            approvedRequest.TransitionTo(
                Guid.NewGuid(),
                OvertimeRequestStatus.Approved,
                Utc(
                    11,
                    10),
                "user-1",
                "admin",
                approvedMinutes:
                    90);

            var rejectedRequest =
                CreateRequest(
                    employeeId,
                    employmentPeriodId,
                    new DateOnly(
                        2026,
                        8,
                        12),
                    90);

            rejectedRequest.TransitionTo(
                Guid.NewGuid(),
                OvertimeRequestStatus.Rejected,
                Utc(
                    12,
                    10),
                "user-1",
                "admin");

            var cancelledRequest =
                CreateRequest(
                    employeeId,
                    employmentPeriodId,
                    new DateOnly(
                        2026,
                        8,
                        13),
                    90);

            cancelledRequest.TransitionTo(
                Guid.NewGuid(),
                OvertimeRequestStatus.Cancelled,
                Utc(
                    13,
                    10),
                "user-1",
                "admin");

            await using HrManagementDbContext dbContext =
                CreateContext();

            dbContext.Employees.Add(
                employee);

            dbContext.EmploymentPeriods.Add(
                employmentPeriod);

            dbContext.OvertimeRequests.AddRange(
                pendingRequest,
                approvedRequest,
                rejectedRequest,
                cancelledRequest);

            await dbContext.SaveChangesAsync();

            return new SeedResult(
                employeeId,
                approvedRequest.Id);
        }

        public HrManagementDbContext CreateContext()
        {
            return new HrManagementDbContext(
                _options);
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

    private static OvertimeRequest CreateRequest(
        Guid employeeId,
        Guid employmentPeriodId,
        DateOnly workDate,
        int requestedMinutes)
    {
        return new OvertimeRequest(
            Guid.NewGuid(),
            employeeId,
            employmentPeriodId,
            workDate,
            requestedMinutes,
            reason:
                "Tăng ca test payroll",
            submittedAtUtc:
                Utc(
                    workDate.Day,
                    8));
    }

    private static DateTime Utc(
        int day,
        int hour)
    {
        return new DateTime(
            2026,
            8,
            day,
            hour,
            0,
            0,
            DateTimeKind.Utc);
    }
}
