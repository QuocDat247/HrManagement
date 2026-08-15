using HrManagement.Application.Organization.Memberships;
using HrManagement.Domain.Employees;
using HrManagement.Domain.Organization.Departments;
using HrManagement.Domain.Organization.Positions;
using HrManagement.Infrastructure.Organization.Memberships;
using HrManagement.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace HrManagement.Tests.Infrastructure;

public sealed class OrganizationMembershipQueryServiceTests
{
    [Fact]
    public async Task GetEmployeesByDepartmentAsync_ReturnsOnlyEmployeesInDepartment()
    {
        await using var connection =
            await CreateOpenConnectionAsync();

        DbContextOptions<HrManagementDbContext> options =
            CreateOptions(connection);

        Guid devDepartmentId =
            Guid.NewGuid();

        Guid hrDepartmentId =
            Guid.NewGuid();

        Guid developerPositionId =
            Guid.NewGuid();

        await using (var dbContext =
                     new HrManagementDbContext(options))
        {
            await dbContext.Database
                .EnsureCreatedAsync();

            dbContext.Departments.AddRange(
                new Department(
                    devDepartmentId,
                    "DEV",
                    "Phát triển phần mềm"),
                new Department(
                    hrDepartmentId,
                    "HR",
                    "Nhân sự"));

            dbContext.Positions.Add(
                new Position(
                    developerPositionId,
                    "DEVELOPER",
                    "Lập trình viên"));

            dbContext.Employees.AddRange(
                CreateEmployee(
                    "EMP001",
                    "Nguyễn Văn An",
                    devDepartmentId,
                    developerPositionId),

                CreateEmployee(
                    "EMP002",
                    "Trần Thị Bình",
                    hrDepartmentId,
                    developerPositionId));

            await dbContext.SaveChangesAsync();
        }

        var service =
            CreateService(options);

        IReadOnlyList<OrganizationEmployeeListItem> result =
            await service.GetEmployeesByDepartmentAsync(
                devDepartmentId);

        OrganizationEmployeeListItem employee =
            Assert.Single(result);

        Assert.Equal(
            "EMP001",
            employee.EmployeeCode);

        Assert.Equal(
            devDepartmentId,
            (
                await ReadEmployeeAsync(
                    options,
                    employee.EmployeeId)
            ).DepartmentId);
    }

    [Fact]
    public async Task GetEmployeesByPositionAsync_ReturnsOnlyEmployeesWithPosition()
    {
        await using var connection =
            await CreateOpenConnectionAsync();

        DbContextOptions<HrManagementDbContext> options =
            CreateOptions(connection);

        Guid departmentId =
            Guid.NewGuid();

        Guid developerPositionId =
            Guid.NewGuid();

        Guid qaPositionId =
            Guid.NewGuid();

        await using (var dbContext =
                     new HrManagementDbContext(options))
        {
            await dbContext.Database
                .EnsureCreatedAsync();

            dbContext.Departments.Add(
                new Department(
                    departmentId,
                    "DEV",
                    "Phát triển phần mềm"));

            dbContext.Positions.AddRange(
                new Position(
                    developerPositionId,
                    "DEV",
                    "Lập trình viên"),
                new Position(
                    qaPositionId,
                    "QA",
                    "Kiểm thử phần mềm"));

            dbContext.Employees.AddRange(
                CreateEmployee(
                    "EMP001",
                    "Nguyễn Văn An",
                    departmentId,
                    developerPositionId),

                CreateEmployee(
                    "EMP002",
                    "Trần Thị Bình",
                    departmentId,
                    qaPositionId));

            await dbContext.SaveChangesAsync();
        }

        var service =
            CreateService(options);

        IReadOnlyList<OrganizationEmployeeListItem> result =
            await service.GetEmployeesByPositionAsync(
                developerPositionId);

        OrganizationEmployeeListItem employee =
            Assert.Single(result);

        Assert.Equal(
            "EMP001",
            employee.EmployeeCode);

        Assert.Equal(
            "Lập trình viên",
            employee.PositionName);
    }

    [Fact]
    public async Task GetEmployeesByDepartmentAsync_UsesMasterNamesAndLegacyFallback()
    {
        await using var connection =
            await CreateOpenConnectionAsync();

        DbContextOptions<HrManagementDbContext> options =
            CreateOptions(connection);

        Guid departmentId =
            Guid.NewGuid();

        await using (var dbContext =
                     new HrManagementDbContext(options))
        {
            await dbContext.Database
                .EnsureCreatedAsync();

            dbContext.Departments.Add(
                new Department(
                    departmentId,
                    "DEV",
                    "Tên phòng ban hiện tại"));

            dbContext.Employees.Add(
                new Employee(
                    Guid.NewGuid(),
                    "EMP001",
                    "Nguyễn Văn An",
                    null,
                    null,
                    null,
                    new DateOnly(2024, 1, 1),

                    // Cố ý khác master-data.
                    "Tên phòng ban legacy",
                    "Chức danh legacy",

                    EmployeeStatus.Active,
                    departmentId: departmentId,
                    positionId: null));

            await dbContext.SaveChangesAsync();
        }

        var service =
            CreateService(options);

        IReadOnlyList<OrganizationEmployeeListItem> result =
            await service.GetEmployeesByDepartmentAsync(
                departmentId);

        OrganizationEmployeeListItem employee =
            Assert.Single(result);

        // Department có master reference:
        // phải dùng master-data hiện tại.
        Assert.Equal(
            "Tên phòng ban hiện tại",
            employee.DepartmentName);

        // PositionId null:
        // phải fallback legacy display string.
        Assert.Equal(
            "Chức danh legacy",
            employee.PositionName);
    }

    [Fact]
    public async Task GetEmployeesByDepartmentAsync_WhenNoEmployees_ReturnsEmpty()
    {
        await using var connection =
            await CreateOpenConnectionAsync();

        DbContextOptions<HrManagementDbContext> options =
            CreateOptions(connection);

        Guid departmentId =
            Guid.NewGuid();

        await using (var dbContext =
                     new HrManagementDbContext(options))
        {
            await dbContext.Database
                .EnsureCreatedAsync();

            dbContext.Departments.Add(
                new Department(
                    departmentId,
                    "EMPTY",
                    "Phòng ban chưa có nhân viên"));

            await dbContext.SaveChangesAsync();
        }

        var service =
            CreateService(options);

        IReadOnlyList<OrganizationEmployeeListItem> result =
            await service.GetEmployeesByDepartmentAsync(
                departmentId);

        Assert.Empty(result);
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

    private static EfOrganizationMembershipQueryService
        CreateService(
            DbContextOptions<HrManagementDbContext> options)
    {
        return new EfOrganizationMembershipQueryService(
            new TestDbContextFactory(options));
    }

    private static Employee CreateEmployee(
        string employeeCode,
        string fullName,
        Guid? departmentId,
        Guid? positionId)
    {
        return new Employee(
            Guid.NewGuid(),
            employeeCode,
            fullName,
            null,
            null,
            null,
            new DateOnly(2024, 1, 1),
            "Legacy Department",
            "Legacy Position",
            EmployeeStatus.Active,
            departmentId: departmentId,
            positionId: positionId);
    }

    private static async Task<Employee> ReadEmployeeAsync(
        DbContextOptions<HrManagementDbContext> options,
        Guid employeeId)
    {
        await using var dbContext =
            new HrManagementDbContext(options);

        return await dbContext.Employees
            .AsNoTracking()
            .SingleAsync(
                employee =>
                    employee.Id == employeeId);
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
