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
        Guid? positionId,
        EmployeeStatus status = EmployeeStatus.Active)
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
            status,
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

    [Fact]
    public void OrganizationStaffingCount_CurrentEmployeeCount_ExcludesInactive()
    {
        var count =
            new OrganizationStaffingCount(
                Guid.NewGuid(),
                ActiveCount: 5,
                OnLeaveCount: 2,
                InactiveCount: 3);

        Assert.Equal(
            7,
            count.CurrentEmployeeCount);

        Assert.Equal(
            10,
            count.TotalLinkedEmployeeCount);
    }

    [Fact]
    public async Task GetDepartmentStaffingCountsAsync_GroupsEmployeesByDepartment()
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
                    "DEV",
                    "Lập trình viên"));

            dbContext.Employees.AddRange(
                // DEV: 2 Active
                CreateEmployee(
                    "EMP001",
                    "Nguyễn Văn An",
                    devDepartmentId,
                    developerPositionId,
                    EmployeeStatus.Active),

                CreateEmployee(
                    "EMP002",
                    "Trần Thị Bình",
                    devDepartmentId,
                    developerPositionId,
                    EmployeeStatus.Active),

                // DEV: 1 OnLeave
                CreateEmployee(
                    "EMP003",
                    "Lê Minh Châu",
                    devDepartmentId,
                    developerPositionId,
                    EmployeeStatus.OnLeave),

                // DEV: 1 Inactive
                CreateEmployee(
                    "EMP004",
                    "Phạm Quốc Dũng",
                    devDepartmentId,
                    developerPositionId,
                    EmployeeStatus.Inactive),

                // HR: 1 Active
                CreateEmployee(
                    "EMP005",
                    "Võ Thu Hà",
                    hrDepartmentId,
                    developerPositionId,
                    EmployeeStatus.Active),

                // Legacy unresolved:
                // không được tính vào department nào.
                CreateEmployee(
                    "EMP006",
                    "Nhân viên legacy",
                    null,
                    null,
                    EmployeeStatus.Active));

            await dbContext.SaveChangesAsync();
        }

        var service =
            CreateService(options);

        IReadOnlyList<OrganizationStaffingCount> result =
            await service
                .GetDepartmentStaffingCountsAsync();

        Assert.Equal(
            2,
            result.Count);

        OrganizationStaffingCount dev =
            Assert.Single(
                result.Where(
                    item =>
                        item.OrganizationId ==
                        devDepartmentId));

        Assert.Equal(
            2,
            dev.ActiveCount);

        Assert.Equal(
            1,
            dev.OnLeaveCount);

        Assert.Equal(
            1,
            dev.InactiveCount);

        Assert.Equal(
            3,
            dev.CurrentEmployeeCount);

        Assert.Equal(
            4,
            dev.TotalLinkedEmployeeCount);

        OrganizationStaffingCount hr =
            Assert.Single(
                result.Where(
                    item =>
                        item.OrganizationId ==
                        hrDepartmentId));

        Assert.Equal(
            1,
            hr.CurrentEmployeeCount);
    }

    [Fact]
    public async Task GetPositionStaffingCountsAsync_GroupsEmployeesByPosition()
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
                    "DEVELOPER",
                    "Lập trình viên"),

                new Position(
                    qaPositionId,
                    "QA",
                    "Kiểm thử phần mềm"));

            dbContext.Employees.AddRange(
                CreateEmployee(
                    "EMP101",
                    "Nguyễn Văn An",
                    departmentId,
                    developerPositionId,
                    EmployeeStatus.Active),

                CreateEmployee(
                    "EMP102",
                    "Trần Thị Bình",
                    departmentId,
                    developerPositionId,
                    EmployeeStatus.OnLeave),

                CreateEmployee(
                    "EMP103",
                    "Lê Minh Châu",
                    departmentId,
                    developerPositionId,
                    EmployeeStatus.Inactive),

                CreateEmployee(
                    "EMP104",
                    "Phạm Quốc Dũng",
                    departmentId,
                    qaPositionId,
                    EmployeeStatus.Active));

            await dbContext.SaveChangesAsync();
        }

        var service =
            CreateService(options);

        IReadOnlyList<OrganizationStaffingCount> result =
            await service
                .GetPositionStaffingCountsAsync();

        Assert.Equal(
            2,
            result.Count);

        OrganizationStaffingCount developer =
            Assert.Single(
                result.Where(
                    item =>
                        item.OrganizationId ==
                        developerPositionId));

        Assert.Equal(
            1,
            developer.ActiveCount);

        Assert.Equal(
            1,
            developer.OnLeaveCount);

        Assert.Equal(
            1,
            developer.InactiveCount);

        Assert.Equal(
            2,
            developer.CurrentEmployeeCount);

        Assert.Equal(
            3,
            developer.TotalLinkedEmployeeCount);

        OrganizationStaffingCount qa =
            Assert.Single(
                result.Where(
                    item =>
                        item.OrganizationId ==
                        qaPositionId));

        Assert.Equal(
            1,
            qa.CurrentEmployeeCount);
    }

    [Fact]
    public async Task GetDepartmentStaffingCountsAsync_WhenNoAssignments_ReturnsEmpty()
    {
        await using var connection =
            await CreateOpenConnectionAsync();

        DbContextOptions<HrManagementDbContext> options =
            CreateOptions(connection);

        await using (var dbContext =
                     new HrManagementDbContext(options))
        {
            await dbContext.Database
                .EnsureCreatedAsync();

            // Employee legacy chưa resolve organization.
            dbContext.Employees.Add(
                CreateEmployee(
                    "EMP-LEGACY",
                    "Nhân viên legacy",
                    null,
                    null,
                    EmployeeStatus.Active));

            await dbContext.SaveChangesAsync();
        }

        var service =
            CreateService(options);

        IReadOnlyList<OrganizationStaffingCount> result =
            await service
                .GetDepartmentStaffingCountsAsync();

        Assert.Empty(
            result);
    }
}
