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

        public Task<Employee?> GetByIdAsync(
            Guid id,
            CancellationToken cancellationToken = default)
        {
            Employee? employee =
                _employees.FirstOrDefault(employee => employee.Id == id);

            return Task.FromResult(employee);
        }

        public Task<Employee?> GetByEmployeeCodeAsync(
            string employeeCode,
            CancellationToken cancellationToken = default)
        {
            Employee? employee =
                _employees.FirstOrDefault(
                    employee =>
                        string.Equals(
                            employee.EmployeeCode,
                            employeeCode,
                            StringComparison.OrdinalIgnoreCase));

            return Task.FromResult(employee);
        }

        public Task AddAsync(
            Employee employee,
            CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public Task UpdateAsync(
        Employee employee,
        CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }

    [Fact]
    public async Task CreateEmployeeAsync_WithValidRequest_CreatesEmployee()
    {
        var repository = new InMemoryEmployeeRepository();
        var service = new EmployeeService(repository);

        var request = new CreateEmployeeRequest(
            EmployeeCode: "EMP100",
            FullName: "Nguyễn Minh Anh",
            Email: "minhanh@example.com",
            PhoneNumber: "0909000000",
            DateOfBirth: new DateOnly(1997, 6, 15),
            HireDate: new DateOnly(2026, 8, 1),
            Department: "Nhân sự",
            Position: "Chuyên viên",
            Status: EmployeeStatus.Active);

        CreateEmployeeResult result =
            await service.CreateEmployeeAsync(request);

        Assert.True(result.IsSuccessful);
        Assert.NotNull(result.EmployeeId);
        Assert.Null(result.ErrorMessage);

        Employee? employee =
            await repository.GetByEmployeeCodeAsync("EMP100");

        Assert.NotNull(employee);
        Assert.Equal("Nguyễn Minh Anh", employee.FullName);
    }

    [Fact]
    public async Task CreateEmployeeAsync_WithDuplicateEmployeeCode_ReturnsFailure()
    {
        var existingEmployee = new Employee(
            Guid.NewGuid(),
            "EMP001",
            "Nguyễn Văn An",
            null,
            null,
            null,
            new DateOnly(2022, 3, 1),
            "Nhân sự",
            "Chuyên viên",
            EmployeeStatus.Active);

        var repository =
            new InMemoryEmployeeRepository(existingEmployee);

        var service = new EmployeeService(repository);

        var request = new CreateEmployeeRequest(
            "EMP001",
            "Nhân viên mới",
            null,
            null,
            null,
            new DateOnly(2026, 8, 1),
            "Nhân sự",
            "Chuyên viên",
            EmployeeStatus.Active);

        CreateEmployeeResult result =
            await service.CreateEmployeeAsync(request);

        Assert.False(result.IsSuccessful);
        Assert.Equal(
            "Mã nhân viên đã tồn tại.",
            result.ErrorMessage);
    }

    [Fact]
    public async Task CreateEmployeeAsync_WithInvalidDomainData_ReturnsFailure()
    {
        var repository = new InMemoryEmployeeRepository();
        var service = new EmployeeService(repository);

        var request = new CreateEmployeeRequest(
            EmployeeCode: "   ",
            FullName: "Nguyễn Văn An",
            Email: null,
            PhoneNumber: null,
            DateOfBirth: null,
            HireDate: new DateOnly(2026, 8, 1),
            Department: "Nhân sự",
            Position: "Chuyên viên",
            Status: EmployeeStatus.Active);

        CreateEmployeeResult result =
            await service.CreateEmployeeAsync(request);

        Assert.False(result.IsSuccessful);
        Assert.NotNull(result.ErrorMessage);
    }

    private sealed class InMemoryEmployeeRepository
    : IEmployeeRepository
    {
        private readonly List<Employee> _employees;

        public InMemoryEmployeeRepository(
            params Employee[] employees)
        {
            _employees = employees.ToList();
        }

        public Task<IReadOnlyList<Employee>> GetAllAsync(
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<Employee>>(
                _employees);
        }

        public Task<Employee?> GetByIdAsync(
            Guid id,
            CancellationToken cancellationToken = default)
        {
            Employee? employee =
                _employees.FirstOrDefault(
                    employee => employee.Id == id);

            return Task.FromResult(employee);
        }

        public Task<Employee?> GetByEmployeeCodeAsync(
            string employeeCode,
            CancellationToken cancellationToken = default)
        {
            Employee? employee =
                _employees.FirstOrDefault(
                    employee =>
                        string.Equals(
                            employee.EmployeeCode,
                            employeeCode.Trim(),
                            StringComparison.OrdinalIgnoreCase));

            return Task.FromResult(employee);
        }

        public Task AddAsync(
            Employee employee,
            CancellationToken cancellationToken = default)
        {
            _employees.Add(employee);

            return Task.CompletedTask;
        }

        public Task UpdateAsync(
        Employee employee,
        CancellationToken cancellationToken = default)
        {
            int index =
                _employees.FindIndex(
                    existingEmployee =>
                        existingEmployee.Id == employee.Id);

            if (index >= 0)
            {
                _employees[index] = employee;
            }

            return Task.CompletedTask;
        }
    }

    [Fact]
    public async Task UpdateEmployeeAsync_WithValidRequest_UpdatesEmployee()
    {
        var existingEmployee = new Employee(
            Guid.NewGuid(),
            "EMP001",
            "Nguyễn Văn An",
            null,
            null,
            null,
            new DateOnly(2022, 3, 1),
            "Nhân sự",
            "Chuyên viên",
            EmployeeStatus.Active);

        var repository =
            new InMemoryEmployeeRepository(existingEmployee);

        var service = new EmployeeService(repository);

        var request = new UpdateEmployeeRequest(
            existingEmployee.Id,
            "EMP001",
            "Nguyễn Văn An Updated",
            "an.updated@example.com",
            "0909000001",
            null,
            new DateOnly(2022, 3, 1),
            "Nhân sự",
            "Chuyên viên cao cấp",
            EmployeeStatus.Active);

        UpdateEmployeeResult result =
            await service.UpdateEmployeeAsync(request);

        Assert.True(result.IsSuccessful);
        Assert.Null(result.ErrorMessage);

        Employee? updated =
            await repository.GetByIdAsync(existingEmployee.Id);

        Assert.NotNull(updated);
        Assert.Equal(
            "Nguyễn Văn An Updated",
            updated.FullName);
        Assert.Equal(
            "Chuyên viên cao cấp",
            updated.Position);
    }

    [Fact]
    public async Task UpdateEmployeeAsync_WhenEmployeeDoesNotExist_ReturnsFailure()
    {
        var repository =
            new InMemoryEmployeeRepository();

        var service = new EmployeeService(repository);

        var request = new UpdateEmployeeRequest(
            Guid.NewGuid(),
            "EMP999",
            "Nhân viên không tồn tại",
            null,
            null,
            null,
            new DateOnly(2026, 8, 1),
            "Nhân sự",
            "Chuyên viên",
            EmployeeStatus.Active);

        UpdateEmployeeResult result =
            await service.UpdateEmployeeAsync(request);

        Assert.False(result.IsSuccessful);
        Assert.Equal(
            "Không tìm thấy nhân viên.",
            result.ErrorMessage);
    }

    [Fact]
    public async Task UpdateEmployeeAsync_WithDuplicateEmployeeCode_ReturnsFailure()
    {
        var firstEmployee = new Employee(
            Guid.NewGuid(),
            "EMP001",
            "Nguyễn Văn An",
            null,
            null,
            null,
            new DateOnly(2022, 3, 1),
            "Nhân sự",
            "Chuyên viên",
            EmployeeStatus.Active);

        var secondEmployee = new Employee(
            Guid.NewGuid(),
            "EMP002",
            "Trần Thị Bình",
            null,
            null,
            null,
            new DateOnly(2023, 1, 1),
            "Kế toán",
            "Kế toán viên",
            EmployeeStatus.Active);

        var repository =
            new InMemoryEmployeeRepository(
                firstEmployee,
                secondEmployee);

        var service = new EmployeeService(repository);

        var request = new UpdateEmployeeRequest(
            firstEmployee.Id,
            "EMP002",
            firstEmployee.FullName,
            firstEmployee.Email,
            firstEmployee.PhoneNumber,
            firstEmployee.DateOfBirth,
            firstEmployee.HireDate,
            firstEmployee.Department,
            firstEmployee.Position,
            firstEmployee.Status);

        UpdateEmployeeResult result =
            await service.UpdateEmployeeAsync(request);

        Assert.False(result.IsSuccessful);
        Assert.Equal(
            "Mã nhân viên đã tồn tại.",
            result.ErrorMessage);
    }

    [Fact]
    public async Task UpdateEmployeeAsync_WithInvalidDomainData_ReturnsFailure()
    {
        var existingEmployee = new Employee(
            Guid.NewGuid(),
            "EMP001",
            "Nguyễn Văn An",
            null,
            null,
            null,
            new DateOnly(2022, 3, 1),
            "Nhân sự",
            "Chuyên viên",
            EmployeeStatus.Active);

        var repository =
            new InMemoryEmployeeRepository(existingEmployee);

        var service = new EmployeeService(repository);

        var request = new UpdateEmployeeRequest(
            existingEmployee.Id,
            "EMP001",
            "   ",
            null,
            null,
            null,
            existingEmployee.HireDate,
            "Nhân sự",
            "Chuyên viên",
            EmployeeStatus.Active);

        UpdateEmployeeResult result =
            await service.UpdateEmployeeAsync(request);

        Assert.False(result.IsSuccessful);
        Assert.NotNull(result.ErrorMessage);
    }

    [Fact]
    public async Task DeactivateEmployeeAsync_WhenActive_SetsStatusToInactive()
    {
        var employee = new Employee(
            Guid.NewGuid(),
            "EMP001",
            "Nguyễn Văn An",
            null,
            null,
            null,
            new DateOnly(2022, 3, 1),
            "Nhân sự",
            "Chuyên viên",
            EmployeeStatus.Active);

        var repository =
            new InMemoryEmployeeRepository(employee);

        var service = new EmployeeService(repository);

        DeactivateEmployeeResult result =
            await service.DeactivateEmployeeAsync(employee.Id);

        Assert.True(result.IsSuccessful);
        Assert.Null(result.ErrorMessage);

        Employee? updated =
            await repository.GetByIdAsync(employee.Id);

        Assert.NotNull(updated);
        Assert.Equal(
            EmployeeStatus.Inactive,
            updated.Status);
    }

    [Fact]
    public async Task DeactivateEmployeeAsync_WhenOnLeave_SetsStatusToInactive()
    {
        var employee = new Employee(
            Guid.NewGuid(),
            "EMP002",
            "Lê Minh Châu",
            null,
            null,
            null,
            new DateOnly(2023, 2, 10),
            "Công nghệ thông tin",
            "Lập trình viên",
            EmployeeStatus.OnLeave);

        var repository =
            new InMemoryEmployeeRepository(employee);

        var service = new EmployeeService(repository);

        DeactivateEmployeeResult result =
            await service.DeactivateEmployeeAsync(employee.Id);

        Assert.True(result.IsSuccessful);

        Employee? updated =
            await repository.GetByIdAsync(employee.Id);

        Assert.NotNull(updated);
        Assert.Equal(
            EmployeeStatus.Inactive,
            updated.Status);
    }

    [Fact]
    public async Task DeactivateEmployeeAsync_WhenEmployeeDoesNotExist_ReturnsFailure()
    {
        var repository =
            new InMemoryEmployeeRepository();

        var service = new EmployeeService(repository);

        DeactivateEmployeeResult result =
            await service.DeactivateEmployeeAsync(
                Guid.NewGuid());

        Assert.False(result.IsSuccessful);
        Assert.Equal(
            "Không tìm thấy nhân viên.",
            result.ErrorMessage);
    }

    [Fact]
    public async Task DeactivateEmployeeAsync_WhenAlreadyInactive_ReturnsSuccess()
    {
        var employee = new Employee(
            Guid.NewGuid(),
            "EMP003",
            "Võ Thu Hà",
            null,
            null,
            null,
            new DateOnly(2019, 6, 20),
            "Hành chính",
            "Chuyên viên hành chính",
            EmployeeStatus.Inactive);

        var repository =
            new InMemoryEmployeeRepository(employee);

        var service = new EmployeeService(repository);

        DeactivateEmployeeResult result =
            await service.DeactivateEmployeeAsync(employee.Id);

        Assert.True(result.IsSuccessful);
        Assert.Null(result.ErrorMessage);

        Employee? unchanged =
            await repository.GetByIdAsync(employee.Id);

        Assert.NotNull(unchanged);
        Assert.Equal(
            EmployeeStatus.Inactive,
            unchanged.Status);
    }
}
