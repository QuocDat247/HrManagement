using HrManagement.Application.Employees;
using HrManagement.Domain.Employees;

namespace HrManagement.Tests.Employees;

public sealed class EmployeeServiceTests
{
    private static readonly IReadOnlyList<Employee> TestEmployees =
    [
        new Employee(
            Guid.NewGuid(),
            "EMP001",
            "Nguyễn Văn An",
            "an@example.com",
            null,
            null,
            new DateOnly(2022, 3, 1),
            "Nhân sự",
            "Chuyên viên nhân sự",
            EmployeeStatus.Active),

        new Employee(
            Guid.NewGuid(),
            "EMP002",
            "Lê Minh Châu",
            "chau@example.com",
            null,
            null,
            new DateOnly(2023, 2, 10),
            "Công nghệ thông tin",
            "Lập trình viên",
            EmployeeStatus.OnLeave),

        new Employee(
            Guid.NewGuid(),
            "EMP003",
            "Phạm Quốc Dũng",
            "dung@example.com",
            null,
            null,
            new DateOnly(2020, 10, 5),
            "Kinh doanh",
            "Trưởng nhóm kinh doanh",
            EmployeeStatus.Active)
    ];

    [Fact]
    public async Task GetEmployeesAsync_WithNoFilter_ReturnsAllEmployees()
    {
        var repository = new StubEmployeeRepository(TestEmployees);
        var service = new EmployeeService(repository);

        IReadOnlyList<Employee> result =
            await service.GetEmployeesAsync();

        Assert.Equal(3, result.Count);
    }

    [Fact]
    public async Task GetEmployeesAsync_WithSearchText_FiltersEmployees()
    {
        var repository = new StubEmployeeRepository(TestEmployees);
        var service = new EmployeeService(repository);

        var filter = new EmployeeFilter(SearchText: "Châu");

        IReadOnlyList<Employee> result =
            await service.GetEmployeesAsync(filter);

        Employee employee = Assert.Single(result);

        Assert.Equal("EMP002", employee.EmployeeCode);
    }

    [Fact]
    public async Task GetEmployeesAsync_SearchIsCaseInsensitive()
    {
        var repository = new StubEmployeeRepository(TestEmployees);
        var service = new EmployeeService(repository);

        var filter = new EmployeeFilter(SearchText: "kinh DOANH");

        IReadOnlyList<Employee> result =
            await service.GetEmployeesAsync(filter);

        Employee employee = Assert.Single(result);

        Assert.Equal("EMP003", employee.EmployeeCode);
    }

    [Fact]
    public async Task GetEmployeesAsync_WithStatus_FiltersEmployees()
    {
        var repository = new StubEmployeeRepository(TestEmployees);
        var service = new EmployeeService(repository);

        var filter = new EmployeeFilter(
            Status: EmployeeStatus.Active);

        IReadOnlyList<Employee> result =
            await service.GetEmployeesAsync(filter);

        Assert.Equal(2, result.Count);

        Assert.All(
            result,
            employee =>
                Assert.Equal(
                    EmployeeStatus.Active,
                    employee.Status));
    }

    [Fact]
    public async Task GetEmployeesAsync_WithSearchAndStatus_AppliesBothFilters()
    {
        var repository = new StubEmployeeRepository(TestEmployees);
        var service = new EmployeeService(repository);

        var filter = new EmployeeFilter(
            SearchText: "Kinh doanh",
            Status: EmployeeStatus.Active);

        IReadOnlyList<Employee> result =
            await service.GetEmployeesAsync(filter);

        Employee employee = Assert.Single(result);

        Assert.Equal("EMP003", employee.EmployeeCode);
    }

    private sealed class StubEmployeeRepository : IEmployeeRepository
    {
        private readonly IReadOnlyList<Employee> _employees;

        public StubEmployeeRepository(
            IReadOnlyList<Employee> employees)
        {
            _employees = employees;
        }

        public Task<IReadOnlyList<Employee>> GetAllAsync(
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_employees);
        }
    }
}
