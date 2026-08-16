using HrManagement.Domain.Employees;
using HrManagement.Domain.Employees.OrganizationAssignments;
using HrManagement.Domain.Organization.Departments;
using HrManagement.Domain.Organization.Positions;
using HrManagement.Infrastructure.Employees;
using HrManagement.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace HrManagement.Tests.Employees;

public sealed class EfEmployeeOrganizationHistoryRepositoryTests
{
    [Fact]
    public async Task GetByEmployeeIdAsync_ReturnsOrderedHistoryAndCurrentAssignment()
    {
        await using SqliteConnection connection =
            await CreateOpenConnectionAsync();

        DbContextOptions<HrManagementDbContext> options =
            CreateOptions(connection);

        SeedData seed =
            await SeedBaseDataAsync(
                options);

        var first =
            new EmployeeOrganizationAssignment(
                Guid.NewGuid(),
                seed.Employee.Id,
                seed.EmploymentPeriod.Id,
                seed.Department.Id,
                seed.Department.Code,
                "Snapshot DEV cũ",
                seed.Position.Id,
                seed.Position.Code,
                "Snapshot Developer cũ",
                new DateOnly(2025, 1, 1),
                new DateOnly(2025, 5, 31));

        var second =
            new EmployeeOrganizationAssignment(
                Guid.NewGuid(),
                seed.Employee.Id,
                seed.EmploymentPeriod.Id,
                seed.SecondDepartment.Id,
                seed.SecondDepartment.Code,
                seed.SecondDepartment.Name,
                seed.SecondPosition.Id,
                seed.SecondPosition.Code,
                seed.SecondPosition.Name,
                new DateOnly(2025, 6, 1));

        await using (var dbContext =
                     new HrManagementDbContext(options))
        {
            // Cố ý thêm ngược thứ tự.
            dbContext.EmployeeOrganizationAssignments
                .AddRange(
                    second,
                    first);

            await dbContext.SaveChangesAsync();
        }

        var repository =
            new EfEmployeeOrganizationHistoryRepository(
                new TestDbContextFactory(options));

        EmployeeOrganizationHistory history =
            await repository.GetByEmployeeIdAsync(
                seed.Employee.Id);

        Assert.Equal(
            2,
            history.Assignments.Count);

        Assert.Equal(
            first.Id,
            history.Assignments[0].Id);

        Assert.Equal(
            second.Id,
            history.Assignments[1].Id);

        Assert.Same(
            history.Assignments[1],
            history.CurrentAssignment);

        // Snapshot phải được đọc đúng,
        // không tự lookup tên master mới.
        Assert.Equal(
            "Snapshot DEV cũ",
            history.Assignments[0].DepartmentName);

        Assert.Equal(
            "Snapshot Developer cũ",
            history.Assignments[0].PositionName);
    }

    [Fact]
    public async Task AddAssignmentAsync_PersistsAssignment()
    {
        await using SqliteConnection connection =
            await CreateOpenConnectionAsync();

        DbContextOptions<HrManagementDbContext> options =
            CreateOptions(connection);

        SeedData seed =
            await SeedBaseDataAsync(
                options);

        var assignment =
            new EmployeeOrganizationAssignment(
                Guid.NewGuid(),
                seed.Employee.Id,
                seed.EmploymentPeriod.Id,
                seed.Department.Id,
                seed.Department.Code,
                seed.Department.Name,
                seed.Position.Id,
                seed.Position.Code,
                seed.Position.Name,
                new DateOnly(2025, 1, 1));

        var repository =
            new EfEmployeeOrganizationHistoryRepository(
                new TestDbContextFactory(options));

        await repository.AddAssignmentAsync(
            assignment);

        await using var dbContext =
            new HrManagementDbContext(options);

        EmployeeOrganizationAssignment persisted =
            await dbContext.EmployeeOrganizationAssignments
                .AsNoTracking()
                .SingleAsync();

        Assert.Equal(
            assignment.Id,
            persisted.Id);

        Assert.Equal(
            seed.Employee.Id,
            persisted.EmployeeId);

        Assert.Equal(
            seed.EmploymentPeriod.Id,
            persisted.EmploymentPeriodId);

        Assert.Equal(
            seed.Department.Id,
            persisted.DepartmentId);

        Assert.Equal(
            seed.Position.Id,
            persisted.PositionId);

        Assert.True(
            persisted.IsOpen);
    }

    [Fact]
    public async Task UpdateAssignmentAsync_PersistsClosedEndDate()
    {
        await using SqliteConnection connection =
            await CreateOpenConnectionAsync();

        DbContextOptions<HrManagementDbContext> options =
            CreateOptions(connection);

        SeedData seed =
            await SeedBaseDataAsync(
                options);

        var assignment =
            new EmployeeOrganizationAssignment(
                Guid.NewGuid(),
                seed.Employee.Id,
                seed.EmploymentPeriod.Id,
                seed.Department.Id,
                seed.Department.Code,
                seed.Department.Name,
                seed.Position.Id,
                seed.Position.Code,
                seed.Position.Name,
                new DateOnly(2025, 1, 1));

        var repository =
            new EfEmployeeOrganizationHistoryRepository(
                new TestDbContextFactory(options));

        await repository.AddAssignmentAsync(
            assignment);

        DateOnly endDate =
            new DateOnly(2025, 8, 31);

        assignment.Close(
            endDate);

        await repository.UpdateAssignmentAsync(
            assignment);

        await using var dbContext =
            new HrManagementDbContext(options);

        EmployeeOrganizationAssignment persisted =
            await dbContext.EmployeeOrganizationAssignments
                .AsNoTracking()
                .SingleAsync();

        Assert.Equal(
            endDate,
            persisted.EndDate);

        Assert.False(
            persisted.IsOpen);
    }

    [Fact]
    public async Task Database_WhenEmployeeHasTwoOpenAssignments_RejectsSecondAssignment()
    {
        await using SqliteConnection connection =
            await CreateOpenConnectionAsync();

        DbContextOptions<HrManagementDbContext> options =
            CreateOptions(connection);

        SeedData seed =
            await SeedBaseDataAsync(
                options);

        var first =
            new EmployeeOrganizationAssignment(
                Guid.NewGuid(),
                seed.Employee.Id,
                seed.EmploymentPeriod.Id,
                seed.Department.Id,
                seed.Department.Code,
                seed.Department.Name,
                seed.Position.Id,
                seed.Position.Code,
                seed.Position.Name,
                new DateOnly(2025, 1, 1));

        var second =
            new EmployeeOrganizationAssignment(
                Guid.NewGuid(),
                seed.Employee.Id,
                seed.EmploymentPeriod.Id,
                seed.SecondDepartment.Id,
                seed.SecondDepartment.Code,
                seed.SecondDepartment.Name,
                seed.SecondPosition.Id,
                seed.SecondPosition.Code,
                seed.SecondPosition.Name,
                new DateOnly(2025, 6, 1));

        var repository =
            new EfEmployeeOrganizationHistoryRepository(
                new TestDbContextFactory(options));

        await repository.AddAssignmentAsync(
            first);

        await Assert.ThrowsAsync<DbUpdateException>(
            () =>
                repository.AddAssignmentAsync(
                    second));
    }

    private static async Task<SeedData>
        SeedBaseDataAsync(
            DbContextOptions<HrManagementDbContext> options)
    {
        var department =
            new Department(
                Guid.NewGuid(),
                "DEV",
                "Phát triển phần mềm");

        var secondDepartment =
            new Department(
                Guid.NewGuid(),
                "RD",
                "Nghiên cứu và phát triển");

        var position =
            new Position(
                Guid.NewGuid(),
                "SWE",
                "Kỹ sư phần mềm");

        var secondPosition =
            new Position(
                Guid.NewGuid(),
                "LEAD",
                "Trưởng nhóm kỹ thuật");

        var employee =
            new Employee(
                Guid.NewGuid(),
                "EMP-ORG-001",
                "Nhân viên Organization",
                null,
                null,
                null,
                new DateOnly(2025, 1, 1),
                department.Name,
                position.Name,
                EmployeeStatus.Active,
                departmentId: department.Id,
                positionId: position.Id);

        var employmentPeriod =
            new EmploymentPeriod(
                Guid.NewGuid(),
                employee.Id,
                new DateOnly(2025, 1, 1));

        await using var dbContext =
            new HrManagementDbContext(options);

        await dbContext.Database
            .EnsureCreatedAsync();

        dbContext.Departments.AddRange(
            department,
            secondDepartment);

        dbContext.Positions.AddRange(
            position,
            secondPosition);

        dbContext.Employees.Add(
            employee);

        dbContext.EmploymentPeriods.Add(
            employmentPeriod);

        await dbContext.SaveChangesAsync();

        return new SeedData(
            employee,
            employmentPeriod,
            department,
            secondDepartment,
            position,
            secondPosition);
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
            .UseSqlite(connection)
            .Options;
    }

    private sealed record SeedData(
        Employee Employee,
        EmploymentPeriod EmploymentPeriod,
        Department Department,
        Department SecondDepartment,
        Position Position,
        Position SecondPosition);

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

        public HrManagementDbContext
            CreateDbContext()
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
