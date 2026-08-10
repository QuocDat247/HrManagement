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
}
