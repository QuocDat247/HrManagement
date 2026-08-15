using HrManagement.Application.Employees;
using HrManagement.Application.Employees.EmploymentHistories;
using HrManagement.Application.Employees.EmploymentLifecycle;
using HrManagement.Application.Organization.Departments;
using HrManagement.Application.Organization.Positions;
using HrManagement.Domain.Employees;
using HrManagement.Domain.Organization.Departments;
using HrManagement.Domain.Organization.Positions;
using Microsoft.EntityFrameworkCore.Migrations;

namespace HrManagement.Tests.Employees;

public sealed class EmployeeServiceTests
{
    private readonly StubDepartmentRepository
    departmentRepository = new();

    private readonly StubPositionRepository
        positionRepository = new();

    private readonly StubEmploymentHistoryRepository
        historyRepository = new();

    private readonly StubEmploymentLifecyclePersistence
        lifecyclePersistence = new();

    private sealed class StubDepartmentRepository
    : IDepartmentRepository
    {
        public List<Department> Departments
        {
            get;
        } = [];

        public Task<Department?> GetByIdAsync(
            Guid departmentId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(
                Departments.FirstOrDefault(
                    department =>
                        department.Id == departmentId));
        }

        public Task<Department?> GetByCodeAsync(
            string code,
            CancellationToken cancellationToken = default)
            => Task.FromResult<Department?>(null);

        public Task<IReadOnlyList<Department>> GetAllAsync(
            CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<Department>>(
                Departments);

        public Task AddAsync(
            Department department,
            CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task UpdateAsync(
            Department department,
            CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }

    private sealed class StubPositionRepository
    : IPositionRepository
    {
        public List<Position> Positions
        {
            get;
        } = [];

        public Task<Position?> GetByIdAsync(
            Guid positionId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(
                Positions.FirstOrDefault(
                    position =>
                        position.Id == positionId));
        }

        public Task<Position?> GetByCodeAsync(
            string code,
            CancellationToken cancellationToken = default)
            => Task.FromResult<Position?>(null);

        public Task<IReadOnlyList<Position>> GetAllAsync(
            CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<Position>>(
                Positions);

        public Task AddAsync(
            Position position,
            CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task UpdateAsync(
            Position position,
            CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }

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
        var service =
    new EmployeeService(
        repository,
        historyRepository,
        lifecyclePersistence,
        departmentRepository,
        positionRepository);

        IReadOnlyList<Employee> result =
            await service.GetEmployeesAsync();

        Assert.Equal(3, result.Count);
    }

    [Fact]
    public async Task GetEmployeesAsync_WithSearchText_FiltersEmployees()
    {
        var repository = new StubEmployeeRepository(TestEmployees);
        var service =
        new EmployeeService(
        repository,
        historyRepository,
        lifecyclePersistence,
        departmentRepository,
        positionRepository);

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
        var service =
        new EmployeeService(
        repository,
        historyRepository,
        lifecyclePersistence,
        departmentRepository,
        positionRepository);

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
        var service =
        new EmployeeService(
        repository,
        historyRepository,
        lifecyclePersistence,
        departmentRepository,
        positionRepository);

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
        var service =
    new EmployeeService(
        repository,
        historyRepository,
        lifecyclePersistence,
        departmentRepository,
        positionRepository);

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
        var department =
        new Department(
        Guid.NewGuid(),
        "IT",
        "Công nghệ thông tin");

        var position =
            new Position(
                Guid.NewGuid(),
                "DEV",
                "Lập trình viên");

        departmentRepository.Departments.Add(
            department);

        positionRepository.Positions.Add(
            position);

        var repository =
            new InMemoryEmployeeRepository();

        var service =
        new EmployeeService(
        repository,
        historyRepository,
        lifecyclePersistence,
        departmentRepository,
        positionRepository);

        var request =
            new CreateEmployeeRequest(
                EmployeeCode: "EMP100",
                FullName: "Nguyễn Minh Anh",
                Email: "minhanh@example.com",
                PhoneNumber: "0909000000",
                DateOfBirth: new DateOnly(1997, 6, 15),
                HireDate: new DateOnly(2026, 8, 1),
                DepartmentId: department.Id,
                PositionId: position.Id,
                Status: EmployeeStatus.Active);

        CreateEmployeeResult result =
            await service.CreateEmployeeAsync(request);

        Assert.True(result.IsSuccessful);
        Assert.NotNull(result.EmployeeId);
        Assert.Null(result.ErrorMessage);

        Employee employee =
            Assert.IsType<Employee>(
                lifecyclePersistence.CreatedEmployee);

        Assert.Equal(
            result.EmployeeId,
            employee.Id);

        Assert.Equal(
            "EMP100",
            employee.EmployeeCode);

        Assert.Equal(
            "Nguyễn Minh Anh",
            employee.FullName);

        Assert.Equal(
            EmployeeStatus.Active,
            employee.Status);

        EmploymentPeriod period =
            Assert.IsType<EmploymentPeriod>(
                lifecyclePersistence.CreatedPeriod);

        Assert.Equal(
            employee.Id,
            period.EmployeeId);

        Assert.Equal(
            new DateOnly(2026, 8, 1),
            period.StartDate);

        Assert.Null(
            period.EndDate);

        Assert.True(
            period.IsOpen);
    }

    [Fact]
    public async Task CreateEmployeeAsync_WithDuplicateEmployeeCode_ReturnsFailure()
    {
        var department =
        new Department(
        Guid.NewGuid(),
        "HR",
        "Nhân sự");

        var position =
            new Position(
                Guid.NewGuid(),
                "SPEC",
                "Chuyên viên");

        departmentRepository.Departments.Add(
            department);

        positionRepository.Positions.Add(
            position);

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

        var service =
        new EmployeeService(
        repository,
        historyRepository,
        lifecyclePersistence,
        departmentRepository,
        positionRepository);

        var request = new CreateEmployeeRequest(
            "EMP001",
            "Nhân viên mới",
            null,
            null,
            null,
            new DateOnly(2026, 8, 1),
            department.Id,
            position.Id,
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
        var department =
        new Department(
        Guid.NewGuid(),
        "HR",
        "Nhân sự");

        var position =
            new Position(
                Guid.NewGuid(),
                "SPEC",
                "Chuyên viên");

        departmentRepository.Departments.Add(
            department);

        positionRepository.Positions.Add(
            position);

        var repository = new InMemoryEmployeeRepository();
        var service =
    new EmployeeService(
        repository,
        historyRepository,
        lifecyclePersistence,
        departmentRepository,
        positionRepository);

        var request = new CreateEmployeeRequest(
            EmployeeCode: "   ",
            FullName: "Nguyễn Văn An",
            Email: null,
            PhoneNumber: null,
            DateOfBirth: null,
            HireDate: new DateOnly(2026, 8, 1),
            DepartmentId: department.Id,
            PositionId: position.Id,
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
        var department =
            new Department(
        Guid.NewGuid(),
        "HR",
        "Nhân sự");

        var position =
            new Position(
                Guid.NewGuid(),
                "SR-SPEC",
                "Chuyên viên cao cấp");

        departmentRepository.Departments.Add(
            department);

        positionRepository.Positions.Add(
            position);

        var existingEmployee =
            new Employee(
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
            new InMemoryEmployeeRepository(
                existingEmployee);

        var organization =
        AddActiveOrganization();

        var service =
        new EmployeeService(
        repository,
        historyRepository,
        lifecyclePersistence,
        departmentRepository,
        positionRepository);

        var request =
            new UpdateEmployeeRequest(
                existingEmployee.Id,
                "EMP001",
                "Nguyễn Văn An Updated",
                "an.updated@example.com",
                "0909000001",
                null,
                new DateOnly(2022, 3, 1),
                department.Id,
                position.Id,
                EmployeeStatus.Active);

        UpdateEmployeeResult result =
            await service.UpdateEmployeeAsync(
                request);

        Assert.True(result.IsSuccessful);
        Assert.Null(result.ErrorMessage);

        Employee? updated =
            await repository.GetByIdAsync(
                existingEmployee.Id);

        Assert.NotNull(updated);

        Assert.Equal(
            "Nguyễn Văn An Updated",
            updated.FullName);

        Assert.Equal(
            "Chuyên viên cao cấp",
            updated.Position);

        Assert.Null(
            lifecyclePersistence.UpdatedEmployee);

        Assert.Null(
            lifecyclePersistence.UpdatedPeriod);
    }

    [Fact]
    public async Task UpdateEmployeeAsync_WhenEmployeeDoesNotExist_ReturnsFailure()
    {
        var department =
    new Department(
        Guid.NewGuid(),
        "HR",
        "Nhân sự");

        var position =
            new Position(
                Guid.NewGuid(),
                "SPEC",
                "Chuyên viên");

        departmentRepository.Departments.Add(
            department);

        positionRepository.Positions.Add(
            position);

        var repository =
            new InMemoryEmployeeRepository();

        var service =
    new EmployeeService(
        repository,
        historyRepository,
        lifecyclePersistence,
        departmentRepository,
        positionRepository);

        var request = new UpdateEmployeeRequest(
            Guid.NewGuid(),
            "EMP999",
            "Nhân viên không tồn tại",
            null,
            null,
            null,
            new DateOnly(2026, 8, 1),
            department.Id,
            position.Id,
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

        var organization =
        AddActiveOrganization(
        firstEmployee.Department,
        firstEmployee.Position);

        var repository =
            new InMemoryEmployeeRepository(
                firstEmployee,
                secondEmployee);

        var service =
    new EmployeeService(
        repository,
        historyRepository,
        lifecyclePersistence,
        departmentRepository,
        positionRepository);

        var request = new UpdateEmployeeRequest(
            firstEmployee.Id,
            "EMP002",
            firstEmployee.FullName,
            firstEmployee.Email,
            firstEmployee.PhoneNumber,
            firstEmployee.DateOfBirth,
            firstEmployee.HireDate,
            organization.Department.Id,
            organization.Position.Id,
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

        var organization =
        AddActiveOrganization(
        "Nhân sự",
        "Chuyên viên");

        var service =
    new EmployeeService(
        repository,
        historyRepository,
        lifecyclePersistence,
        departmentRepository,
        positionRepository);

        var request = new UpdateEmployeeRequest(
            existingEmployee.Id,
            "EMP001",
            "   ",
            null,
            null,
            null,
            existingEmployee.HireDate,
            organization.Department.Id,
            organization.Position.Id,
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

        var historyRepository =
            new StubEmploymentHistoryRepository
            {
                History = new EmploymentHistory(
                    employee.Id,
                    [
                        new EmploymentPeriod(
                        Guid.NewGuid(),
                        employee.Id,
                        employee.HireDate)
                    ])
            };

        var lifecyclePersistence =
            new StubEmploymentLifecyclePersistence();

        var service =
    new EmployeeService(
        repository,
        historyRepository,
        lifecyclePersistence,
        departmentRepository,
        positionRepository);

        DateOnly terminationDate =
            new(2026, 8, 1);

        DeactivateEmployeeResult result =
            await service.DeactivateEmployeeAsync(
                employee.Id,
                terminationDate);

        Assert.True(result.IsSuccessful);
        Assert.Null(result.ErrorMessage);

        Employee updatedEmployee =
            Assert.IsType<Employee>(
                lifecyclePersistence.UpdatedEmployee);

        EmploymentPeriod updatedPeriod =
            Assert.IsType<EmploymentPeriod>(
                lifecyclePersistence.UpdatedPeriod);

        Assert.Equal(
            EmployeeStatus.Inactive,
            updatedEmployee.Status);

        Assert.Equal(
            terminationDate,
            updatedEmployee.TerminationDate);

        Assert.Equal(
            employee.Id,
            updatedPeriod.EmployeeId);

        Assert.Equal(
            employee.HireDate,
            updatedPeriod.StartDate);

        Assert.Equal(
            terminationDate,
            updatedPeriod.EndDate);

        Assert.False(
            updatedPeriod.IsOpen);
    }

    [Fact]
    public async Task DeactivateEmployeeAsync_PreservesOrganizationReferences()
    {
        Guid departmentId =
            Guid.NewGuid();

        Guid positionId =
            Guid.NewGuid();

        var employee =
            new Employee(
                Guid.NewGuid(),
                "EMP-ORG-DEACTIVATE",
                "Nhân viên kiểm thử",
                null,
                null,
                null,
                new DateOnly(2024, 1, 1),
                "Công nghệ thông tin",
                "Lập trình viên",
                EmployeeStatus.Active,
                departmentId: departmentId,
                positionId: positionId);

        var repository =
            new InMemoryEmployeeRepository(
                employee);

        var historyRepository =
            new StubEmploymentHistoryRepository
            {
                History =
                    new EmploymentHistory(
                        employee.Id,
                        [
                            new EmploymentPeriod(
                            Guid.NewGuid(),
                            employee.Id,
                            employee.HireDate)
                        ])
            };

        var lifecyclePersistence =
            new StubEmploymentLifecyclePersistence();

        var service =
            new EmployeeService(
                repository,
                historyRepository,
                lifecyclePersistence,
                departmentRepository,
                positionRepository);

        DateOnly terminationDate =
            new(2026, 8, 1);

        DeactivateEmployeeResult result =
            await service.DeactivateEmployeeAsync(
                employee.Id,
                terminationDate);

        Assert.True(
            result.IsSuccessful);

        Employee updatedEmployee =
            Assert.IsType<Employee>(
                lifecyclePersistence.UpdatedEmployee);

        Assert.Equal(
            departmentId,
            updatedEmployee.DepartmentId);

        Assert.Equal(
            positionId,
            updatedEmployee.PositionId);
    }

    [Fact]
    public async Task CancelDeactivationAsync_PreservesOrganizationReferences()
    {
        Guid departmentId =
            Guid.NewGuid();

        Guid positionId =
            Guid.NewGuid();

        DateOnly terminationDate =
            new(2026, 6, 15);

        var employee =
            new Employee(
                Guid.NewGuid(),
                "EMP-ORG-CANCEL",
                "Nhân viên kiểm thử",
                null,
                null,
                null,
                new DateOnly(2024, 1, 1),
                "Công nghệ thông tin",
                "Lập trình viên",
                EmployeeStatus.Inactive,
                terminationDate,
                departmentId: departmentId,
                positionId: positionId);

        var repository =
            new InMemoryEmployeeRepository(
                employee);

        var historyRepository =
            new StubEmploymentHistoryRepository
            {
                History =
                    new EmploymentHistory(
                        employee.Id,
                        [
                            new EmploymentPeriod(
                            Guid.NewGuid(),
                            employee.Id,
                            employee.HireDate,
                            terminationDate)
                        ])
            };

        var lifecyclePersistence =
            new StubEmploymentLifecyclePersistence();

        var service =
            new EmployeeService(
                repository,
                historyRepository,
                lifecyclePersistence,
                departmentRepository,
                positionRepository);

        CancelEmployeeDeactivationResult result =
            await service.CancelDeactivationAsync(
                employee.Id,
                EmployeeStatus.Active);

        Assert.True(
            result.IsSuccessful);

        Employee restoredEmployee =
            Assert.IsType<Employee>(
                lifecyclePersistence.UpdatedEmployee);

        Assert.Equal(
            departmentId,
            restoredEmployee.DepartmentId);

        Assert.Equal(
            positionId,
            restoredEmployee.PositionId);
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

        var historyRepository =
            new StubEmploymentHistoryRepository
            {
                History = new EmploymentHistory(
                    employee.Id,
                    [
                        new EmploymentPeriod(
                        Guid.NewGuid(),
                        employee.Id,
                        employee.HireDate)
                    ])
            };

        var lifecyclePersistence =
            new StubEmploymentLifecyclePersistence();

        var service =
    new EmployeeService(
        repository,
        historyRepository,
        lifecyclePersistence,
        departmentRepository,
        positionRepository);

        DateOnly terminationDate =
            new(2026, 8, 1);

        DeactivateEmployeeResult result =
            await service.DeactivateEmployeeAsync(
                employee.Id,
                terminationDate);

        Assert.True(result.IsSuccessful);
        Assert.Null(result.ErrorMessage);

        Employee updatedEmployee =
            Assert.IsType<Employee>(
                lifecyclePersistence.UpdatedEmployee);

        EmploymentPeriod updatedPeriod =
            Assert.IsType<EmploymentPeriod>(
                lifecyclePersistence.UpdatedPeriod);

        Assert.Equal(
            EmployeeStatus.Inactive,
            updatedEmployee.Status);

        Assert.Equal(
            terminationDate,
            updatedEmployee.TerminationDate);

        Assert.Equal(
            terminationDate,
            updatedPeriod.EndDate);

        Assert.False(
            updatedPeriod.IsOpen);
    }

    [Fact]
    public async Task DeactivateEmployeeAsync_WhenEmployeeDoesNotExist_ReturnsFailure()
    {
        var repository =
            new InMemoryEmployeeRepository();

        var service =
    new EmployeeService(
        repository,
        historyRepository,
        lifecyclePersistence,
        departmentRepository,
        positionRepository);

        DeactivateEmployeeResult result =
            await service.DeactivateEmployeeAsync(
                Guid.NewGuid());

        Assert.False(result.IsSuccessful);

        Assert.Equal(
            "Không tìm thấy nhân viên.",
            result.ErrorMessage);

        Assert.Null(
            lifecyclePersistence.UpdatedEmployee);

        Assert.Null(
            lifecyclePersistence.UpdatedPeriod);
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

        var service =
    new EmployeeService(
        repository,
        historyRepository,
        lifecyclePersistence,
        departmentRepository,
        positionRepository);

        DeactivateEmployeeResult result =
            await service.DeactivateEmployeeAsync(
                employee.Id);

        Assert.True(result.IsSuccessful);
        Assert.Null(result.ErrorMessage);

        Employee? unchanged =
            await repository.GetByIdAsync(
                employee.Id);

        Assert.NotNull(unchanged);

        Assert.Equal(
            EmployeeStatus.Inactive,
            unchanged.Status);

        Assert.Null(
            unchanged.TerminationDate);

        Assert.Null(
            lifecyclePersistence.UpdatedEmployee);

        Assert.Null(
            lifecyclePersistence.UpdatedPeriod);
    }

    [Fact]
    public async Task GetEmployeesAsync_WhenProfileCompletionFilterEnabled_ReturnsOnlyEmployeesRequiringCompletion()
    {
        var completeEmployee = new Employee(
            Guid.NewGuid(),
            "EMP001",
            "Nguyễn Văn An",
            "an@example.com",
            "0901000001",
            new DateOnly(1995, 5, 20),
            new DateOnly(2022, 3, 1),
            "Nhân sự",
            "Chuyên viên",
            EmployeeStatus.Active);

        var incompleteActiveEmployee = new Employee(
            Guid.NewGuid(),
            "EMP002",
            "Trần Thị Bình",
            null,
            "0901000002",
            new DateOnly(1994, 7, 10),
            new DateOnly(2023, 1, 1),
            "Kế toán",
            "Kế toán viên",
            EmployeeStatus.Active);

        var incompleteInactiveEmployee = new Employee(
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
            new InMemoryEmployeeRepository(
                completeEmployee,
                incompleteActiveEmployee,
                incompleteInactiveEmployee);

        var service =
    new EmployeeService(
        repository,
        historyRepository,
        lifecyclePersistence,
        departmentRepository,
        positionRepository);

        IReadOnlyList<Employee> result =
            await service.GetEmployeesAsync(
                new EmployeeFilter(
                    RequiresProfileCompletionOnly: true));

        Employee employee =
            Assert.Single(result);

        Assert.Equal(
            incompleteActiveEmployee.Id,
            employee.Id);

        Assert.True(
            employee.RequiresProfileCompletion);
    }

    [Fact]
    public async Task DeactivateEmployeeAsync_WhenTerminationDateIsMissing_ReturnsFailure()
    {
        var employee = new Employee(
            Guid.NewGuid(),
            "EMP100",
            "Nhân viên kiểm thử",
            null,
            null,
            null,
            new DateOnly(2024, 1, 1),
            "Nhân sự",
            "Chuyên viên",
            EmployeeStatus.Active);

        var repository =
            new InMemoryEmployeeRepository(employee);

        var service =
    new EmployeeService(
        repository,
        historyRepository,
        lifecyclePersistence,
        departmentRepository,
        positionRepository);

        DeactivateEmployeeResult result =
            await service.DeactivateEmployeeAsync(employee.Id);

        Assert.False(result.IsSuccessful);
        Assert.Equal(
            "Vui lòng chọn ngày nghỉ việc.",
            result.ErrorMessage);

        Employee? unchanged =
            await repository.GetByIdAsync(employee.Id);

        Assert.NotNull(unchanged);
        Assert.Equal(EmployeeStatus.Active, unchanged.Status);
        Assert.Null(unchanged.TerminationDate);
    }

    [Fact]
public async Task DeactivateEmployeeAsync_WhenTerminationDateIsInFuture_ReturnsFailure()
{
    var employee =
        new Employee(
            Guid.NewGuid(),
            "EMP101",
            "Nhân viên kiểm thử",
            null,
            null,
            null,
            new DateOnly(2024, 1, 1),
            "Nhân sự",
            "Chuyên viên",
            EmployeeStatus.Active);

    var repository =
        new InMemoryEmployeeRepository(employee);

        var service =
        new EmployeeService(
            repository,
            historyRepository,
            lifecyclePersistence,
            departmentRepository,
            positionRepository);

        DateOnly tomorrow =
        DateOnly.FromDateTime(DateTime.Today)
            .AddDays(1);

    DeactivateEmployeeResult result =
        await service.DeactivateEmployeeAsync(
            employee.Id,
            tomorrow);

    Assert.False(result.IsSuccessful);

    Assert.Equal(
        "Ngày nghỉ việc không thể ở tương lai.",
        result.ErrorMessage);

    Assert.Null(
        lifecyclePersistence.UpdatedEmployee);

    Assert.Null(
        lifecyclePersistence.UpdatedPeriod);
}

    [Fact]
    public async Task UpdateEmployeeAsync_WhenChangingActiveToInactive_ReturnsFailure()
    {
        var employee = new Employee(
            Guid.NewGuid(),
            "EMP102",
            "Nhân viên kiểm thử",
            null,
            null,
            null,
            new DateOnly(2024, 1, 1),
            "Nhân sự",
            "Chuyên viên",
            EmployeeStatus.Active);

        var repository =
            new InMemoryEmployeeRepository(employee);

        var organization =
    AddActiveOrganization(
        employee.Department,
        employee.Position);

        var service =
    new EmployeeService(
        repository,
        historyRepository,
        lifecyclePersistence,
        departmentRepository,
        positionRepository);

        var request = new UpdateEmployeeRequest(
            employee.Id,
            employee.EmployeeCode,
            employee.FullName,
            employee.Email,
            employee.PhoneNumber,
            employee.DateOfBirth,
            employee.HireDate,
            organization.Department.Id,
            organization.Position.Id,
            EmployeeStatus.Inactive);

        UpdateEmployeeResult result =
            await service.UpdateEmployeeAsync(request);

        Assert.False(result.IsSuccessful);
        Assert.Equal(
            "Vui lòng sử dụng chức năng Ngừng hoạt động để ghi nhận ngày nghỉ việc.",
            result.ErrorMessage);
    }

    [Fact]
    public async Task UpdateEmployeeAsync_WhenInactive_PreservesTerminationDate()
    {
        DateOnly terminationDate =
            new DateOnly(2026, 7, 31);

        var employee = new Employee(
            Guid.NewGuid(),
            "EMP103",
            "Nhân viên kiểm thử",
            null,
            null,
            null,
            new DateOnly(2024, 1, 1),
            "Nhân sự",
            "Chuyên viên",
            EmployeeStatus.Inactive,
            terminationDate);

        var repository =
            new InMemoryEmployeeRepository(employee);

        var organization =
    AddActiveOrganization(
        employee.Department,
        employee.Position);

        var service =
    new EmployeeService(
        repository,
        historyRepository,
        lifecyclePersistence,
        departmentRepository,
        positionRepository);

        var request = new UpdateEmployeeRequest(
            employee.Id,
            employee.EmployeeCode,
            "Nhân viên đã cập nhật",
            employee.Email,
            employee.PhoneNumber,
            employee.DateOfBirth,
            employee.HireDate,
            organization.Department.Id,
            organization.Position.Id,
            EmployeeStatus.Inactive);

        UpdateEmployeeResult result =
            await service.UpdateEmployeeAsync(request);

        Assert.True(result.IsSuccessful);

        Employee? updated =
            await repository.GetByIdAsync(employee.Id);

        Assert.NotNull(updated);

        Assert.Equal(
            terminationDate,
            updated.TerminationDate);
    }

    [Fact]
    public async Task CreateEmployeeAsync_WhenStatusIsInactive_ReturnsFailure()
    {
        var repository =
            new InMemoryEmployeeRepository();

        var department =
            new Department(
                Guid.NewGuid(),
                "HR",
                "Nhân sự");

        var position =
            new Position(
                Guid.NewGuid(),
                "SPEC",
                "Chuyên viên");

        departmentRepository.Departments.Add(
            department);

        positionRepository.Positions.Add(
            position);


        departmentRepository.Departments.Add(
            department);

        positionRepository.Positions.Add(
            position);

        var service =
    new EmployeeService(
        repository,
        historyRepository,
        lifecyclePersistence,
        departmentRepository,
        positionRepository);

        var request = new CreateEmployeeRequest(
            "EMP1004",
            "Nhân viên kiểm thử",
            null,
            null,
            null,
            new DateOnly(2026, 1, 1),
            department.Id,
            position.Id,
            EmployeeStatus.Inactive);

        CreateEmployeeResult result =
            await service.CreateEmployeeAsync(request);

        Assert.False(result.IsSuccessful);
        Assert.Equal(
            "Không thể tạo mới nhân viên ở trạng thái ngừng hoạt động.",
            result.ErrorMessage);
    }

    private sealed class StubEmploymentHistoryRepository
    : IEmploymentHistoryRepository
    {
        public EmploymentHistory? History { get; set; }

        public Task<EmploymentHistory>
            GetByEmployeeIdAsync(
                Guid employeeId,
                CancellationToken cancellationToken = default)
        {
            return Task.FromResult(
                History
                ?? new EmploymentHistory(
                    employeeId,
                    []));
        }

        public Task AddPeriodAsync(
            EmploymentPeriod period,
            CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public Task UpdatePeriodAsync(
            EmploymentPeriod period,
            CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }

    private sealed class StubEmploymentLifecyclePersistence
    : IEmploymentLifecyclePersistence
    {
        public Employee? RehiredEmployee
        {
            get;
            private set;
        }

        public EmploymentPeriod? RehirePeriod
        {
            get;
            private set;
        }

        public Task UpdateEmployeeWithNewPeriodAsync(
            Employee employee,
            EmploymentPeriod newPeriod,
            CancellationToken cancellationToken = default)
        {
            RehiredEmployee =
                employee;

            RehirePeriod =
                newPeriod;

            return Task.CompletedTask;
        }

        public Employee? CreatedEmployee { get; private set; }

        public EmploymentPeriod? CreatedPeriod { get; private set; }

        public Employee? UpdatedEmployee { get; private set; }

        public EmploymentPeriod? UpdatedPeriod { get; private set; }

        public Task CreateEmployeeWithPeriodAsync(
            Employee employee,
            EmploymentPeriod period,
            CancellationToken cancellationToken = default)
        {
            CreatedEmployee = employee;
            CreatedPeriod = period;

            return Task.CompletedTask;
        }

        public Task UpdateEmployeeWithPeriodAsync(
            Employee employee,
            EmploymentPeriod period,
            CancellationToken cancellationToken = default)
        {
            UpdatedEmployee = employee;
            UpdatedPeriod = period;

            return Task.CompletedTask;
        }
    }

    [Fact]
    public async Task DeactivateEmployeeAsync_WhenNoOpenEmploymentPeriodExists_ReturnsFailure()
    {
        var employee =
            new Employee(
                Guid.NewGuid(),
                "EMP-NO-PERIOD",
                "Nhân viên không có kỳ làm việc",
                null,
                null,
                null,
                new DateOnly(2024, 1, 1),
                "Nhân sự",
                "Chuyên viên",
                EmployeeStatus.Active);

        var repository =
            new InMemoryEmployeeRepository(
                employee);

        historyRepository.History =
            new EmploymentHistory(
                employee.Id,
                []);

        var service =
    new EmployeeService(
        repository,
        historyRepository,
        lifecyclePersistence,
        departmentRepository,
        positionRepository);

        DateOnly terminationDate =
            new(2026, 8, 1);

        DeactivateEmployeeResult result =
            await service.DeactivateEmployeeAsync(
                employee.Id,
                terminationDate);

        Assert.False(
            result.IsSuccessful);

        Assert.Equal(
            "Không tìm thấy giai đoạn làm việc đang mở của nhân viên.",
            result.ErrorMessage);

        Assert.Null(
            lifecyclePersistence.UpdatedEmployee);

        Assert.Null(
            lifecyclePersistence.UpdatedPeriod);

        Employee? unchanged =
            await repository.GetByIdAsync(
                employee.Id);

        Assert.NotNull(unchanged);

        Assert.Equal(
            EmployeeStatus.Active,
            unchanged.Status);

        Assert.Null(
            unchanged.TerminationDate);
    }

    [Fact]
    public async Task CancelDeactivationAsync_WhenValid_RestoresEmployeeToActive()
    {
        DateOnly terminationDate =
            new(2026, 6, 15);

        var employee =
            new Employee(
                Guid.NewGuid(),
                "EMP-CANCEL-001",
                "Nguyễn Văn An",
                null,
                null,
                null,
                new DateOnly(2024, 1, 1),
                "Nhân sự",
                "Chuyên viên",
                EmployeeStatus.Inactive,
                terminationDate);

        var repository =
            new InMemoryEmployeeRepository(
                employee);

        historyRepository.History =
            new EmploymentHistory(
                employee.Id,
                [
                    new EmploymentPeriod(
                    Guid.NewGuid(),
                    employee.Id,
                    employee.HireDate,
                    terminationDate)
                ]);

        var service =
    new EmployeeService(
        repository,
        historyRepository,
        lifecyclePersistence,
        departmentRepository,
        positionRepository);

        CancelEmployeeDeactivationResult result =
            await service.CancelDeactivationAsync(
                employee.Id,
                EmployeeStatus.Active);

        Assert.True(result.IsSuccessful);
        Assert.Null(result.ErrorMessage);

        Employee restoredEmployee =
            Assert.IsType<Employee>(
                lifecyclePersistence.UpdatedEmployee);

        EmploymentPeriod reopenedPeriod =
            Assert.IsType<EmploymentPeriod>(
                lifecyclePersistence.UpdatedPeriod);

        Assert.Equal(
            EmployeeStatus.Active,
            restoredEmployee.Status);

        Assert.Null(
            restoredEmployee.TerminationDate);

        Assert.Null(
            reopenedPeriod.EndDate);

        Assert.True(
            reopenedPeriod.IsOpen);
    }

    [Fact]
    public async Task CancelDeactivationAsync_WhenRestoredStatusIsOnLeave_RestoresEmployeeToOnLeave()
    {
        DateOnly terminationDate =
            new(2026, 6, 15);

        var employee =
            new Employee(
                Guid.NewGuid(),
                "EMP-CANCEL-002",
                "Lê Minh Châu",
                null,
                null,
                null,
                new DateOnly(2024, 2, 1),
                "Công nghệ thông tin",
                "Lập trình viên",
                EmployeeStatus.Inactive,
                terminationDate);

        var repository =
            new InMemoryEmployeeRepository(
                employee);

        historyRepository.History =
            new EmploymentHistory(
                employee.Id,
                [
                    new EmploymentPeriod(
                    Guid.NewGuid(),
                    employee.Id,
                    employee.HireDate,
                    terminationDate)
                ]);

        var service =
    new EmployeeService(
        repository,
        historyRepository,
        lifecyclePersistence,
        departmentRepository,
        positionRepository);

        CancelEmployeeDeactivationResult result =
            await service.CancelDeactivationAsync(
                employee.Id,
                EmployeeStatus.OnLeave);

        Assert.True(result.IsSuccessful);

        Assert.Equal(
            EmployeeStatus.OnLeave,
            lifecyclePersistence
                .UpdatedEmployee!
                .Status);

        Assert.Null(
            lifecyclePersistence
                .UpdatedEmployee
                .TerminationDate);

        Assert.True(
            lifecyclePersistence
                .UpdatedPeriod!
                .IsOpen);
    }

    [Fact]
    public async Task CancelDeactivationAsync_WhenEmployeeDoesNotExist_ReturnsFailure()
    {
        var repository =
            new InMemoryEmployeeRepository();

        var service =
    new EmployeeService(
        repository,
        historyRepository,
        lifecyclePersistence,
        departmentRepository,
        positionRepository);

        CancelEmployeeDeactivationResult result =
            await service.CancelDeactivationAsync(
                Guid.NewGuid(),
                EmployeeStatus.Active);

        Assert.False(result.IsSuccessful);

        Assert.Equal(
            "Không tìm thấy nhân viên.",
            result.ErrorMessage);

        Assert.Null(
            lifecyclePersistence.UpdatedEmployee);
    }

    [Fact]
    public async Task CancelDeactivationAsync_WhenEmployeeIsNotInactive_ReturnsFailure()
    {
        var employee =
            new Employee(
                Guid.NewGuid(),
                "EMP-CANCEL-003",
                "Nhân viên đang làm việc",
                null,
                null,
                null,
                new DateOnly(2024, 1, 1),
                "Nhân sự",
                "Chuyên viên",
                EmployeeStatus.Active);

        var repository =
            new InMemoryEmployeeRepository(
                employee);

        var service =
    new EmployeeService(
        repository,
        historyRepository,
        lifecyclePersistence,
        departmentRepository,
        positionRepository);

        CancelEmployeeDeactivationResult result =
            await service.CancelDeactivationAsync(
                employee.Id,
                EmployeeStatus.Active);

        Assert.False(result.IsSuccessful);

        Assert.Null(
            lifecyclePersistence.UpdatedEmployee);
    }

    [Fact]
    public async Task CancelDeactivationAsync_WhenTerminationDateIsMissing_ReturnsFailure()
    {
        var employee =
            new Employee(
                Guid.NewGuid(),
                "EMP-CANCEL-004",
                "Nhân viên legacy",
                null,
                null,
                null,
                new DateOnly(2020, 1, 1),
                "Hành chính",
                "Chuyên viên",
                EmployeeStatus.Inactive);

        var repository =
            new InMemoryEmployeeRepository(
                employee);

        var service =
    new EmployeeService(
        repository,
        historyRepository,
        lifecyclePersistence,
        departmentRepository,
        positionRepository);

        CancelEmployeeDeactivationResult result =
            await service.CancelDeactivationAsync(
                employee.Id,
                EmployeeStatus.Active);

        Assert.False(result.IsSuccessful);

        Assert.Equal(
            "Không thể hủy ngừng hoạt động vì hồ sơ chưa có ngày nghỉ việc.",
            result.ErrorMessage);

        Assert.Null(
            lifecyclePersistence.UpdatedEmployee);

        Assert.Null(
            lifecyclePersistence.UpdatedPeriod);
    }

    [Fact]
    public async Task CancelDeactivationAsync_WhenHistoryTerminationDateDoesNotMatch_ReturnsFailure()
    {
        DateOnly employeeTerminationDate =
            new(2026, 6, 15);

        var employee =
            new Employee(
                Guid.NewGuid(),
                "EMP-CANCEL-005",
                "Nhân viên lệch lịch sử",
                null,
                null,
                null,
                new DateOnly(2024, 1, 1),
                "Nhân sự",
                "Chuyên viên",
                EmployeeStatus.Inactive,
                employeeTerminationDate);

        var repository =
            new InMemoryEmployeeRepository(
                employee);

        historyRepository.History =
            new EmploymentHistory(
                employee.Id,
                [
                    new EmploymentPeriod(
                    Guid.NewGuid(),
                    employee.Id,
                    employee.HireDate,
                    new DateOnly(2026, 6, 20))
                ]);

        var service =
    new EmployeeService(
        repository,
        historyRepository,
        lifecyclePersistence,
        departmentRepository,
        positionRepository);

        CancelEmployeeDeactivationResult result =
            await service.CancelDeactivationAsync(
                employee.Id,
                EmployeeStatus.Active);

        Assert.False(result.IsSuccessful);

        Assert.Equal(
            "Ngày kết thúc của lịch sử làm việc không khớp.",
            result.ErrorMessage);

        Assert.Null(
            lifecyclePersistence.UpdatedEmployee);

        Assert.Null(
            lifecyclePersistence.UpdatedPeriod);
    }

    [Fact]
    public async Task CancelDeactivationAsync_WhenRestoredStatusIsInvalid_ReturnsFailure()
    {
        DateOnly terminationDate =
            new(2026, 6, 15);

        var employee =
            new Employee(
                Guid.NewGuid(),
                "EMP-CANCEL-006",
                "Nhân viên kiểm thử",
                null,
                null,
                null,
                new DateOnly(2024, 1, 1),
                "Nhân sự",
                "Chuyên viên",
                EmployeeStatus.Inactive,
                terminationDate);

        var repository =
            new InMemoryEmployeeRepository(
                employee);

        var service =
    new EmployeeService(
        repository,
        historyRepository,
        lifecyclePersistence,
        departmentRepository,
        positionRepository);

        CancelEmployeeDeactivationResult result =
            await service.CancelDeactivationAsync(
                employee.Id,
                EmployeeStatus.Inactive);

        Assert.False(result.IsSuccessful);

        Assert.Equal(
            "Trạng thái khôi phục phải là Đang làm việc hoặc Nghỉ phép.",
            result.ErrorMessage);

        Assert.Null(
            lifecyclePersistence.UpdatedEmployee);
    }

    [Fact]
    public async Task RehireEmployeeAsync_WhenValid_RestoresEmployeeAndCreatesNewPeriod()
    {
        DateOnly originalHireDate =
            new(2022, 1, 10);

        DateOnly terminationDate =
            new(2026, 3, 15);

        DateOnly rehireDate =
            new(2026, 8, 1);

        var employee =
            new Employee(
                Guid.NewGuid(),
                "EMP-REHIRE-001",
                "Nguyễn Văn An",
                null,
                null,
                null,
                originalHireDate,
                "Nhân sự",
                "Chuyên viên",
                EmployeeStatus.Inactive,
                terminationDate);

        var repository =
            new InMemoryEmployeeRepository(
                employee);

        EmploymentPeriod previousPeriod =
            new(
                Guid.NewGuid(),
                employee.Id,
                originalHireDate,
                terminationDate);

        historyRepository.History =
            new EmploymentHistory(
                employee.Id,
                [previousPeriod]);

        var service =
    new EmployeeService(
        repository,
        historyRepository,
        lifecyclePersistence,
        departmentRepository,
        positionRepository);

        RehireEmployeeResult result =
            await service.RehireEmployeeAsync(
                employee.Id,
                rehireDate,
                EmployeeStatus.Active);

        Assert.True(result.IsSuccessful);
        Assert.Null(result.ErrorMessage);

        Employee rehiredEmployee =
            Assert.IsType<Employee>(
                lifecyclePersistence.RehiredEmployee);

        EmploymentPeriod newPeriod =
            Assert.IsType<EmploymentPeriod>(
                lifecyclePersistence.RehirePeriod);

        Assert.Equal(
            EmployeeStatus.Active,
            rehiredEmployee.Status);

        Assert.Null(
            rehiredEmployee.TerminationDate);

        Assert.Equal(
            originalHireDate,
            rehiredEmployee.HireDate);

        Assert.Equal(
            rehireDate,
            newPeriod.StartDate);

        Assert.Null(
            newPeriod.EndDate);

        Assert.True(
            newPeriod.IsOpen);

        // Period cũ tuyệt đối không bị sửa.
        Assert.Equal(
            terminationDate,
            previousPeriod.EndDate);
    }

    [Fact]
    public async Task RehireEmployeeAsync_WhenStatusIsOnLeave_RestoresEmployeeToOnLeave()
    {
        DateOnly terminationDate =
            new(2026, 3, 15);

        var employee =
            new Employee(
                Guid.NewGuid(),
                "EMP-REHIRE-002",
                "Lê Minh Châu",
                null,
                null,
                null,
                new DateOnly(2023, 1, 1),
                "CNTT",
                "Lập trình viên",
                EmployeeStatus.Inactive,
                terminationDate);

        var repository =
            new InMemoryEmployeeRepository(
                employee);

        historyRepository.History =
            new EmploymentHistory(
                employee.Id,
                [
                    new EmploymentPeriod(
                    Guid.NewGuid(),
                    employee.Id,
                    employee.HireDate,
                    terminationDate)
                ]);

        var service =
    new EmployeeService(
        repository,
        historyRepository,
        lifecyclePersistence,
        departmentRepository,
        positionRepository);

        RehireEmployeeResult result =
            await service.RehireEmployeeAsync(
                employee.Id,
                new DateOnly(2026, 8, 1),
                EmployeeStatus.OnLeave);

        Assert.True(result.IsSuccessful);

        Assert.Equal(
            EmployeeStatus.OnLeave,
            lifecyclePersistence
                .RehiredEmployee!
                .Status);

        Assert.True(
            lifecyclePersistence
                .RehirePeriod!
                .IsOpen);
    }

    [Fact]
    public async Task RehireEmployeeAsync_WhenEmployeeIsNotInactive_ReturnsFailure()
    {
        var employee =
            new Employee(
                Guid.NewGuid(),
                "EMP-REHIRE-003",
                "Nhân viên đang làm việc",
                null,
                null,
                null,
                new DateOnly(2024, 1, 1),
                "Nhân sự",
                "Chuyên viên",
                EmployeeStatus.Active);

        var repository =
            new InMemoryEmployeeRepository(
                employee);

        var service =
    new EmployeeService(
        repository,
        historyRepository,
        lifecyclePersistence,
        departmentRepository,
        positionRepository);

        RehireEmployeeResult result =
            await service.RehireEmployeeAsync(
                employee.Id,
                new DateOnly(2026, 8, 1),
                EmployeeStatus.Active);

        Assert.False(result.IsSuccessful);

        Assert.Equal(
            "Chỉ có thể tái tuyển dụng nhân viên đã ngừng hoạt động.",
            result.ErrorMessage);

        Assert.Null(
            lifecyclePersistence.RehiredEmployee);
    }

    [Fact]
    public async Task RehireEmployeeAsync_WhenTerminationDateIsMissing_ReturnsFailure()
    {
        var employee =
            new Employee(
                Guid.NewGuid(),
                "EMP-REHIRE-004",
                "Nhân viên legacy",
                null,
                null,
                null,
                new DateOnly(2020, 1, 1),
                "Hành chính",
                "Chuyên viên",
                EmployeeStatus.Inactive);

        var repository =
            new InMemoryEmployeeRepository(
                employee);

        var service =
    new EmployeeService(
        repository,
        historyRepository,
        lifecyclePersistence,
        departmentRepository,
        positionRepository);

        RehireEmployeeResult result =
            await service.RehireEmployeeAsync(
                employee.Id,
                new DateOnly(2026, 8, 1),
                EmployeeStatus.Active);

        Assert.False(result.IsSuccessful);

        Assert.Equal(
            "Không thể tái tuyển dụng vì hồ sơ chưa có ngày nghỉ việc.",
            result.ErrorMessage);

        Assert.Null(
            lifecyclePersistence.RehiredEmployee);
    }

    [Fact]
    public async Task RehireEmployeeAsync_WhenStatusIsInvalid_ReturnsFailure()
    {
        DateOnly terminationDate =
            new(2026, 3, 15);

        var employee =
            new Employee(
                Guid.NewGuid(),
                "EMP-REHIRE-005",
                "Nhân viên kiểm thử",
                null,
                null,
                null,
                new DateOnly(2024, 1, 1),
                "Nhân sự",
                "Chuyên viên",
                EmployeeStatus.Inactive,
                terminationDate);

        var repository =
            new InMemoryEmployeeRepository(
                employee);

        var service =
    new EmployeeService(
        repository,
        historyRepository,
        lifecyclePersistence,
        departmentRepository,
        positionRepository);

        RehireEmployeeResult result =
            await service.RehireEmployeeAsync(
                employee.Id,
                new DateOnly(2026, 8, 1),
                EmployeeStatus.Inactive);

        Assert.False(result.IsSuccessful);

        Assert.Equal(
            "Trạng thái tái tuyển dụng phải là Đang làm việc hoặc Nghỉ phép.",
            result.ErrorMessage);
    }

    [Fact]
    public async Task RehireEmployeeAsync_WhenRehireDateIsInFuture_ReturnsFailure()
    {
        DateOnly terminationDate =
            DateOnly.FromDateTime(
                DateTime.Today)
            .AddDays(-10);

        var employee =
            new Employee(
                Guid.NewGuid(),
                "EMP-REHIRE-006",
                "Nhân viên kiểm thử",
                null,
                null,
                null,
                new DateOnly(2024, 1, 1),
                "Nhân sự",
                "Chuyên viên",
                EmployeeStatus.Inactive,
                terminationDate);

        var repository =
            new InMemoryEmployeeRepository(
                employee);

        var service =
    new EmployeeService(
        repository,
        historyRepository,
        lifecyclePersistence,
        departmentRepository,
        positionRepository);

        DateOnly tomorrow =
            DateOnly.FromDateTime(
                DateTime.Today)
            .AddDays(1);

        RehireEmployeeResult result =
            await service.RehireEmployeeAsync(
                employee.Id,
                tomorrow,
                EmployeeStatus.Active);

        Assert.False(result.IsSuccessful);

        Assert.Equal(
            "Ngày tái tuyển dụng không thể ở tương lai.",
            result.ErrorMessage);

        Assert.Null(
            lifecyclePersistence.RehiredEmployee);
    }

    [Fact]
    public async Task RehireEmployeeAsync_WhenHistoryTerminationDateDoesNotMatch_ReturnsFailure()
    {
        DateOnly terminationDate =
            new(2026, 3, 15);

        var employee =
            new Employee(
                Guid.NewGuid(),
                "EMP-REHIRE-007",
                "Nhân viên lệch lịch sử",
                null,
                null,
                null,
                new DateOnly(2024, 1, 1),
                "Nhân sự",
                "Chuyên viên",
                EmployeeStatus.Inactive,
                terminationDate);

        var repository =
            new InMemoryEmployeeRepository(
                employee);

        historyRepository.History =
            new EmploymentHistory(
                employee.Id,
                [
                    new EmploymentPeriod(
                    Guid.NewGuid(),
                    employee.Id,
                    employee.HireDate,
                    new DateOnly(2026, 3, 20))
                ]);

        var service =
    new EmployeeService(
        repository,
        historyRepository,
        lifecyclePersistence,
        departmentRepository,
        positionRepository);

        RehireEmployeeResult result =
            await service.RehireEmployeeAsync(
                employee.Id,
                new DateOnly(2026, 8, 1),
                EmployeeStatus.Active);

        Assert.False(result.IsSuccessful);

        Assert.Equal(
            "Ngày kết thúc của lịch sử làm việc không khớp.",
            result.ErrorMessage);

        Assert.Null(
            lifecyclePersistence.RehiredEmployee);

        Assert.Null(
            lifecyclePersistence.RehirePeriod);
    }

    private (
    Department Department,
    Position Position)
    AddActiveOrganization()
    {
        var department =
            new Department(
                Guid.NewGuid(),
                "HR",
                "Nhân sự");

        var position =
            new Position(
                Guid.NewGuid(),
                "SPEC",
                "Chuyên viên");

        departmentRepository.Departments.Add(
            department);

        positionRepository.Positions.Add(
            position);

        return (
            department,
            position);
    }

    private (
    Department Department,
    Position Position)
    AddActiveOrganization(
        string departmentName,
        string positionName)
    {
        var department =
            new Department(
                Guid.NewGuid(),
                Guid.NewGuid().ToString("N"),
                departmentName);

        var position =
            new Position(
                Guid.NewGuid(),
                Guid.NewGuid().ToString("N"),
                positionName);

        departmentRepository.Departments.Add(
            department);

        positionRepository.Positions.Add(
            position);

        return (
            department,
            position);
    }
}
