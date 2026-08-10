using HrManagement.Domain.Employees;
using HrManagement.Infrastructure.Employees;
using HrManagement.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace HrManagement.Tests.Persistence;

public sealed class EfEmployeeRepositoryTests
{
    [Fact]
    public async Task GetAllAsync_ReturnsEmployeesOrderedByEmployeeCode()
    {
        await using var connection =
            new SqliteConnection("Data Source=:memory:");

        await connection.OpenAsync();

        var options =
            new DbContextOptionsBuilder<HrManagementDbContext>()
                .UseSqlite(connection)
                .Options;

        await using (var dbContext =
                     new HrManagementDbContext(options))
        {
            await dbContext.Database.EnsureCreatedAsync();

            dbContext.Employees.AddRange(
                CreateEmployee("EMP002", "Trần Thị Bình"),
                CreateEmployee("EMP001", "Nguyễn Văn An"));

            await dbContext.SaveChangesAsync();
        }

        var factory =
            new TestDbContextFactory(options);

        var repository =
            new EfEmployeeRepository(factory);

        IReadOnlyList<Employee> employees =
            await repository.GetAllAsync();

        Assert.Equal(2, employees.Count);
        Assert.Equal("EMP001", employees[0].EmployeeCode);
        Assert.Equal("EMP002", employees[1].EmployeeCode);
    }

    private static Employee CreateEmployee(
        string employeeCode,
        string fullName)
    {
        return new Employee(
            Guid.NewGuid(),
            employeeCode,
            fullName,
            null,
            null,
            null,
            new DateOnly(2024, 1, 1),
            "Nhân sự",
            "Chuyên viên",
            EmployeeStatus.Active);
    }

    private sealed class TestDbContextFactory
        : IDbContextFactory<HrManagementDbContext>
    {
        private readonly DbContextOptions<HrManagementDbContext>
            _options;

        public TestDbContextFactory(
            DbContextOptions<HrManagementDbContext> options)
        {
            _options = options;
        }

        public HrManagementDbContext CreateDbContext()
        {
            return new HrManagementDbContext(_options);
        }

        public Task<HrManagementDbContext> CreateDbContextAsync(
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(
                new HrManagementDbContext(_options));
        }
    }

    [Fact]
    public async Task AddAsync_PersistsEmployeeToDatabase()
    {
        await using var connection =
            new SqliteConnection("Data Source=:memory:");

        await connection.OpenAsync();

        var options =
            new DbContextOptionsBuilder<HrManagementDbContext>()
                .UseSqlite(connection)
                .Options;

        await using (var dbContext =
                     new HrManagementDbContext(options))
        {
            await dbContext.Database.EnsureCreatedAsync();
        }

        var factory =
            new TestDbContextFactory(options);

        var repository =
            new EfEmployeeRepository(factory);

        var employee = new Employee(
            Guid.NewGuid(),
            "EMP100",
            "Nguyễn Minh Anh",
            null,
            null,
            null,
            new DateOnly(2026, 8, 1),
            "Nhân sự",
            "Chuyên viên",
            EmployeeStatus.Active);

        await repository.AddAsync(employee);

        Employee? savedEmployee =
            await repository.GetByEmployeeCodeAsync("EMP100");

        Assert.NotNull(savedEmployee);
        Assert.Equal(employee.Id, savedEmployee.Id);
        Assert.Equal("Nguyễn Minh Anh", savedEmployee.FullName);
    }

    [Fact]
    public async Task UpdateAsync_PersistsChangesToDatabase()
    {
        await using var connection =
            new SqliteConnection("Data Source=:memory:");

        await connection.OpenAsync();

        var options =
            new DbContextOptionsBuilder<HrManagementDbContext>()
                .UseSqlite(connection)
                .Options;

        await using (var dbContext =
                     new HrManagementDbContext(options))
        {
            await dbContext.Database.EnsureCreatedAsync();
        }

        var factory =
            new TestDbContextFactory(options);

        var repository =
            new EfEmployeeRepository(factory);

        Guid employeeId = Guid.NewGuid();

        var employee = new Employee(
            employeeId,
            "EMP100",
            "Nguyễn Minh Anh",
            null,
            null,
            null,
            new DateOnly(2026, 8, 1),
            "Nhân sự",
            "Chuyên viên",
            EmployeeStatus.Active);

        await repository.AddAsync(employee);

        var updatedEmployee = new Employee(
            employeeId,
            "EMP100",
            "Nguyễn Minh Anh Updated",
            "updated@example.com",
            null,
            null,
            new DateOnly(2026, 8, 1),
            "Công nghệ thông tin",
            "Lập trình viên",
            EmployeeStatus.Active);

        await repository.UpdateAsync(updatedEmployee);

        Employee? savedEmployee =
            await repository.GetByIdAsync(employeeId);

        Assert.NotNull(savedEmployee);
        Assert.Equal(
            "Nguyễn Minh Anh Updated",
            savedEmployee.FullName);
        Assert.Equal(
            "Công nghệ thông tin",
            savedEmployee.Department);
        Assert.Equal(
            "Lập trình viên",
            savedEmployee.Position);
    }
}
