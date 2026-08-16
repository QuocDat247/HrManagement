using HrManagement.Application.Employees.OrganizationAssignments;
using HrManagement.Domain.Employees;
using HrManagement.Domain.Employees.OrganizationAssignments;
using HrManagement.Domain.Organization.Departments;
using HrManagement.Domain.Organization.Positions;
using HrManagement.Infrastructure.Employees;
using HrManagement.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace HrManagement.Tests.Employees;

public sealed class
    EfEmployeeOrganizationAssignmentBackfillServiceTests
{
    [Fact]
    public async Task BackfillAsync_ForActiveEmployee_CreatesOpenBaselineAtEmploymentPeriodStart()
    {
        await using SqliteConnection connection =
            await CreateOpenConnectionAsync();

        DbContextOptions<HrManagementDbContext> options =
            CreateOptions(
                connection);

        SeedData seed =
            await SeedAsync(
                options,
                EmployeeStatus.Active);

        var service =
            new EfEmployeeOrganizationAssignmentBackfillService(
                new TestDbContextFactory(
                    options));

        EmployeeOrganizationAssignmentBackfillResult result =
            await service.BackfillAsync();

        Assert.Equal(
            1,
            result.ScannedEmployees);

        Assert.Equal(
            1,
            result.CreatedAssignments);

        Assert.Equal(
            0,
            result.SkippedExistingHistory);

        await using var dbContext =
            new HrManagementDbContext(
                options);

        EmployeeOrganizationAssignment assignment =
            await dbContext
                .EmployeeOrganizationAssignments
                .AsNoTracking()
                .SingleAsync();

        Assert.Equal(
            seed.Employee.Id,
            assignment.EmployeeId);

        Assert.Equal(
            seed.EmploymentPeriod.Id,
            assignment.EmploymentPeriodId);

        Assert.Equal(
            seed.Department.Id,
            assignment.DepartmentId);

        Assert.Equal(
            seed.Department.Code,
            assignment.DepartmentCode);

        Assert.Equal(
            seed.Department.Name,
            assignment.DepartmentName);

        Assert.Equal(
            seed.Position.Id,
            assignment.PositionId);

        Assert.Equal(
            seed.Position.Code,
            assignment.PositionCode);

        Assert.Equal(
            seed.Position.Name,
            assignment.PositionName);

        Assert.Equal(
            seed.EmploymentPeriod.StartDate,
            assignment.StartDate);

        Assert.Null(
            assignment.EndDate);

        Assert.True(
            assignment.IsOpen);

        Assert.True(
            assignment.IsBaseline);
    }

    [Fact]
    public async Task BackfillAsync_ForInactiveEmployee_CreatesClosedBaselineForLatestEmploymentPeriod()
    {
        await using SqliteConnection connection =
            await CreateOpenConnectionAsync();

        DbContextOptions<HrManagementDbContext> options =
            CreateOptions(
                connection);

        SeedData seed =
            await SeedAsync(
                options,
                EmployeeStatus.Inactive);

        var service =
            new EfEmployeeOrganizationAssignmentBackfillService(
                new TestDbContextFactory(
                    options));

        EmployeeOrganizationAssignmentBackfillResult result =
            await service.BackfillAsync();

        Assert.Equal(
            1,
            result.CreatedAssignments);

        await using var dbContext =
            new HrManagementDbContext(
                options);

        EmployeeOrganizationAssignment assignment =
            await dbContext
                .EmployeeOrganizationAssignments
                .AsNoTracking()
                .SingleAsync();

        Assert.Equal(
            seed.EmploymentPeriod.StartDate,
            assignment.StartDate);

        Assert.Equal(
            seed.EmploymentPeriod.EndDate,
            assignment.EndDate);

        Assert.False(
            assignment.IsOpen);

        Assert.True(
            assignment.IsBaseline);

        Assert.Equal(
            seed.EmploymentPeriod.Id,
            assignment.EmploymentPeriodId);
    }

    [Fact]
    public async Task BackfillAsync_WhenOrganizationReferencesAreMissing_SkipsEmployee()
    {
        await using SqliteConnection connection =
            await CreateOpenConnectionAsync();

        DbContextOptions<HrManagementDbContext> options =
            CreateOptions(
                connection);

        await SeedAsync(
            options,
            EmployeeStatus.Active,
            includeOrganizationReferences: false);

        var service =
            new EfEmployeeOrganizationAssignmentBackfillService(
                new TestDbContextFactory(
                    options));

        EmployeeOrganizationAssignmentBackfillResult result =
            await service.BackfillAsync();

        Assert.Equal(
            1,
            result.ScannedEmployees);

        Assert.Equal(
            0,
            result.CreatedAssignments);

        Assert.Equal(
            1,
            result.SkippedMissingOrganizationReferences);

        await using var dbContext =
            new HrManagementDbContext(
                options);

        Assert.Empty(
            await dbContext
                .EmployeeOrganizationAssignments
                .ToListAsync());
    }

    [Fact]
    public async Task BackfillAsync_WhenRunTwice_IsIdempotent()
    {
        await using SqliteConnection connection =
            await CreateOpenConnectionAsync();

        DbContextOptions<HrManagementDbContext> options =
            CreateOptions(
                connection);

        await SeedAsync(
            options,
            EmployeeStatus.Active);

        var service =
            new EfEmployeeOrganizationAssignmentBackfillService(
                new TestDbContextFactory(
                    options));

        EmployeeOrganizationAssignmentBackfillResult first =
            await service.BackfillAsync();

        EmployeeOrganizationAssignmentBackfillResult second =
            await service.BackfillAsync();

        Assert.Equal(
            1,
            first.CreatedAssignments);

        Assert.Equal(
            0,
            second.CreatedAssignments);

        Assert.Equal(
            1,
            second.SkippedExistingHistory);

        await using var dbContext =
            new HrManagementDbContext(
                options);

        Assert.Equal(
            1,
            await dbContext
                .EmployeeOrganizationAssignments
                .CountAsync());
    }

    [Fact]
    public async Task BackfillAsync_WhenActiveEmployeeHasNoOpenEmploymentPeriod_SkipsInconsistentState()
    {
        await using SqliteConnection connection =
            await CreateOpenConnectionAsync();

        DbContextOptions<HrManagementDbContext> options =
            CreateOptions(
                connection);

        await SeedAsync(
            options,
            EmployeeStatus.Active,
            employmentPeriodIsOpen: false);

        var service =
            new EfEmployeeOrganizationAssignmentBackfillService(
                new TestDbContextFactory(
                    options));

        EmployeeOrganizationAssignmentBackfillResult result =
            await service.BackfillAsync();

        Assert.Equal(
            1,
            result.ScannedEmployees);

        Assert.Equal(
            0,
            result.CreatedAssignments);

        Assert.Equal(
            1,
            result.SkippedInconsistentEmploymentState);

        await using var dbContext =
            new HrManagementDbContext(
                options);

        Assert.Empty(
            await dbContext
                .EmployeeOrganizationAssignments
                .ToListAsync());
    }

    private static async Task<SeedData> SeedAsync(
        DbContextOptions<HrManagementDbContext> options,
        EmployeeStatus status,
        bool includeOrganizationReferences = true,
        bool employmentPeriodIsOpen = true)
    {
        DateOnly today =
            DateOnly.FromDateTime(
                DateTime.Today);

        DateOnly hireDate =
            today.AddYears(-1);

        DateOnly terminationDate =
            today.AddDays(-10);

        var department =
            new Department(
                Guid.NewGuid(),
                "DEV",
                "Phát triển phần mềm");

        var position =
            new Position(
                Guid.NewGuid(),
                "SWE",
                "Kỹ sư phần mềm");

        DateOnly? employeeTerminationDate =
            status == EmployeeStatus.Inactive
                ? terminationDate
                : null;

        var employee =
            new Employee(
                Guid.NewGuid(),
                "EMP-ORG-BF-001",
                "Nhân viên baseline",
                "baseline@example.com",
                "0901000000",
                new DateOnly(1995, 1, 1),
                hireDate,
                department.Name,
                position.Name,
                status,
                employeeTerminationDate,
                departmentId:
                    includeOrganizationReferences
                        ? department.Id
                        : null,
                positionId:
                    includeOrganizationReferences
                        ? position.Id
                        : null);

        EmploymentPeriod employmentPeriod;

        if (status == EmployeeStatus.Inactive)
        {
            employmentPeriod =
                new EmploymentPeriod(
                    Guid.NewGuid(),
                    employee.Id,
                    hireDate,
                    terminationDate);
        }
        else if (employmentPeriodIsOpen)
        {
            employmentPeriod =
                new EmploymentPeriod(
                    Guid.NewGuid(),
                    employee.Id,
                    hireDate);
        }
        else
        {
            employmentPeriod =
                new EmploymentPeriod(
                    Guid.NewGuid(),
                    employee.Id,
                    hireDate,
                    today.AddDays(-1));
        }

        await using var dbContext =
            new HrManagementDbContext(
                options);

        await dbContext.Database
            .EnsureCreatedAsync();

        dbContext.Departments.Add(
            department);

        dbContext.Positions.Add(
            position);

        dbContext.Employees.Add(
            employee);

        dbContext.EmploymentPeriods.Add(
            employmentPeriod);

        await dbContext.SaveChangesAsync();

        return new SeedData(
            employee,
            employmentPeriod,
            department,
            position);
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

    private sealed record SeedData(
        Employee Employee,
        EmploymentPeriod EmploymentPeriod,
        Department Department,
        Position Position);

    private sealed class TestDbContextFactory
        : IDbContextFactory<HrManagementDbContext>
    {
        private readonly
            DbContextOptions<HrManagementDbContext>
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
