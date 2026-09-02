using HrManagement.Application.Payroll.Compensation;
using HrManagement.Domain.Employees;
using HrManagement.Domain.Payroll.Compensation;
using HrManagement.Infrastructure.Payroll.Compensation;
using HrManagement.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace HrManagement.Tests.Payroll;

public sealed class EmployeeCompensationQuerySourceTests
{
    [Fact]
    public async Task GetForPeriodAsync_ReturnsOnlySegmentsOverlappingRequestedPeriod()
    {
        await using TestDatabase database =
            await TestDatabase.CreateAsync();

        SeedResult seed =
            await database.SeedAsync();

        IReadOnlyList<EmployeeCompensationSegment> rows =
            await database.QuerySource
                .GetForPeriodAsync(
                    [seed.EmployeeId],
                    new DateOnly(
                        2026,
                        8,
                        1),
                    new DateOnly(
                        2026,
                        8,
                        31));

        Assert.Equal(
            2,
            rows.Count);

        Assert.Equal(
            25_000_000m,
            rows[0].MonthlyBaseSalary);

        Assert.Equal(
            new DateOnly(
                2026,
                8,
                15),
            rows[0].EffectiveTo);

        Assert.Equal(
            28_000_000m,
            rows[1].MonthlyBaseSalary);

        Assert.Equal(
            new DateOnly(
                2026,
                8,
                16),
            rows[1].EffectiveFrom);

        Assert.Null(
            rows[1].EffectiveTo);
    }

    [Fact]
    public async Task GetForPeriodAsync_WhenEmployeeListIsEmpty_ReturnsEmpty()
    {
        await using TestDatabase database =
            await TestDatabase.CreateAsync();

        IReadOnlyList<EmployeeCompensationSegment> rows =
            await database.QuerySource
                .GetForPeriodAsync(
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

    [Fact]
    public async Task GetHistoryAsync_ReturnsNewestCompensationFirst()
    {
        await using TestDatabase database =
            await TestDatabase.CreateAsync();

        SeedResult seed =
            await database.SeedAsync();

        IReadOnlyList<EmployeeCompensationSegment> history =
            await database.QuerySource
                .GetHistoryAsync(
                    seed.EmployeeId);

        Assert.Equal(
            2,
            history.Count);

        Assert.Equal(
            28_000_000m,
            history[0].MonthlyBaseSalary);

        Assert.Equal(
            25_000_000m,
            history[1].MonthlyBaseSalary);
    }

    private sealed record SeedResult(
        Guid EmployeeId);

    private sealed class TestDatabase
        : IAsyncDisposable
    {
        private readonly SqliteConnection
            _connection;

        private readonly DbContextOptions<HrManagementDbContext>
            _options;

        public EfEmployeeCompensationQuerySource QuerySource
        {
            get;
        }

        private TestDatabase(
            SqliteConnection connection,
            DbContextOptions<HrManagementDbContext> options,
            EfEmployeeCompensationQuerySource querySource)
        {
            _connection =
                connection;

            _options =
                options;

            QuerySource =
                querySource;
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
                new EfEmployeeCompensationQuerySource(
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

            var first =
                new EmployeeCompensation(
                    Guid.NewGuid(),
                    employeeId,
                    employmentPeriodId,
                    new DateOnly(
                        2026,
                        8,
                        1),
                    25_000_000m,
                    "VND",
                    new DateOnly(
                        2026,
                        8,
                        15));

            var second =
                new EmployeeCompensation(
                    Guid.NewGuid(),
                    employeeId,
                    employmentPeriodId,
                    new DateOnly(
                        2026,
                        8,
                        16),
                    28_000_000m,
                    "VND");

            await using HrManagementDbContext dbContext =
                new(
                    _options);

            dbContext.Employees.Add(
                employee);

            dbContext.EmploymentPeriods.Add(
                employmentPeriod);

            dbContext.EmployeeCompensations.AddRange(
                first,
                second);

            await dbContext.SaveChangesAsync();

            return new SeedResult(
                employeeId);
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
}
