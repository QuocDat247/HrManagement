using HrManagement.Application.Employees;
using HrManagement.Application.Organization.Departments;
using HrManagement.Application.Organization.Positions;
using HrManagement.Desktop.ViewModels;
using HrManagement.Domain.Employees;
using HrManagement.Domain.Organization.Departments;
using HrManagement.Domain.Organization.Positions;

namespace HrManagement.Tests.Employees;

public sealed class EditEmployeeViewModelTests
{
    [Fact]
    public void LoadEmployee_PopulatesEditableFields()
    {
        var service = new StubEmployeeService();
        var viewModel =
    CreateViewModel(service);

        var employee = CreateEmployee();

        viewModel.LoadEmployee(employee);

        Assert.Equal(employee.EmployeeCode, viewModel.EmployeeCode);
        Assert.Equal(employee.FullName, viewModel.FullName);
        Assert.Equal(employee.Email, viewModel.Email);
        Assert.Equal(employee.PhoneNumber, viewModel.PhoneNumber);

        Assert.Equal(
            employee.DateOfBirth?.ToDateTime(TimeOnly.MinValue),
            viewModel.DateOfBirth);

        Assert.Equal(
            employee.HireDate.ToDateTime(TimeOnly.MinValue),
            viewModel.HireDate);

        Assert.Equal(employee.Status, viewModel.SelectedStatus);
    }

    [Fact]
    public async Task SaveCommand_WhenUpdateSucceeds_RaisesSaveSucceeded()
    {
        var service = new StubEmployeeService
        {
            UpdateResult =
                new UpdateEmployeeResult(IsSuccessful: true)
        };

        var viewModel =
    CreateViewModel(service);

        var employee =
            CreateEmployee();

        viewModel.LoadEmployee(employee);

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

        viewModel.SelectedDepartment =
            department;

        viewModel.SelectedPosition =
            position;

        viewModel.FullName =
            "Nguyễn Văn An Updated";

        bool saveSucceeded = false;

        viewModel.SaveSucceeded += (_, _) =>
            saveSucceeded = true;

        await viewModel.SaveCommand.ExecuteAsync(null);

        Assert.True(saveSucceeded);
        Assert.Null(viewModel.ErrorMessage);

        Assert.NotNull(
            service.LastUpdateRequest);

        Assert.Equal(
            employee.Id,
            service.LastUpdateRequest.EmployeeId);

        Assert.Equal(
            "Nguyễn Văn An Updated",
            service.LastUpdateRequest.FullName);

        Assert.Equal(
            department.Id,
            service.LastUpdateRequest.DepartmentId);

        Assert.Equal(
            position.Id,
            service.LastUpdateRequest.PositionId);
    }

    [Fact]
    public async Task SaveCommand_WhenUpdateFails_SetsErrorAndDoesNotRaiseSuccess()
    {
        var service = new StubEmployeeService
        {
            UpdateResult =
                new UpdateEmployeeResult(
                    IsSuccessful: false,
                    ErrorMessage: "Mã nhân viên đã tồn tại.")
        };

        var viewModel =
            CreateViewModel(service);

        viewModel.LoadEmployee(
            CreateEmployee());

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

        viewModel.SelectedDepartment =
            department;

        viewModel.SelectedPosition =
            position;

        bool saveSucceeded = false;

        viewModel.SaveSucceeded += (_, _) =>
            saveSucceeded = true;

        await viewModel.SaveCommand.ExecuteAsync(null);

        Assert.False(saveSucceeded);

        Assert.Equal(
            "Mã nhân viên đã tồn tại.",
            viewModel.ErrorMessage);
    }

    private static Employee CreateEmployee()
    {
        return new Employee(
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
    }

    private sealed class StubEmployeeService : IEmployeeService
    {
        public Task<RehireEmployeeResult> RehireEmployeeAsync(
            Guid employeeId,
            DateOnly rehireDate,
            EmployeeStatus rehireStatus,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(
                new RehireEmployeeResult(
                    false,
                    "Not used in this test."));
        }

        public Task<CancelEmployeeDeactivationResult>
        CancelDeactivationAsync(
        Guid employeeId,
        EmployeeStatus restoredStatus,
        CancellationToken cancellationToken = default)
        {
            return Task.FromResult(
                new CancelEmployeeDeactivationResult(
                    false,
                    "Not used in this test."));
        }
        public Task<DeactivateEmployeeResult> DeactivateEmployeeAsync(
            Guid employeeId,
            DateOnly? terminationDate = null,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(
                new DeactivateEmployeeResult(
                    IsSuccessful: false,
                    ErrorMessage: "Test failure"));
        }

        public UpdateEmployeeResult UpdateResult { get; set; } =
            new(IsSuccessful: true);

        public UpdateEmployeeRequest? LastUpdateRequest { get; private set; }

        public Task<IReadOnlyList<Employee>> GetEmployeesAsync(
            EmployeeFilter? filter = null,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<Employee>>(
                Array.Empty<Employee>());
        }

        public Task<CreateEmployeeResult> CreateEmployeeAsync(
            CreateEmployeeRequest request,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(
                new CreateEmployeeResult(
                    IsSuccessful: true,
                    EmployeeId: Guid.NewGuid()));
        }

        public Task<UpdateEmployeeResult> UpdateEmployeeAsync(
            UpdateEmployeeRequest request,
            CancellationToken cancellationToken = default)
        {
            LastUpdateRequest = request;

            return Task.FromResult(UpdateResult);
        }
    }

    [Fact]
    public void LoadEmployee_WhenActive_AllowsActiveAndOnLeaveStatuses()
    {
        var service = new StubEmployeeService();
        var viewModel =
            CreateViewModel(service);

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

        viewModel.LoadEmployee(employee);

        Assert.Equal(
            [EmployeeStatus.Active, EmployeeStatus.OnLeave],
            viewModel.StatusOptions);

        Assert.Equal(
            EmployeeStatus.Active,
            viewModel.SelectedStatus);
    }

    [Fact]
    public void LoadEmployee_WhenInactive_AllowsOnlyInactiveStatus()
    {
        var service = new StubEmployeeService();
        var viewModel =
            CreateViewModel(service);

        DateOnly terminationDate =
            new DateOnly(2026, 8, 1);

        var employee = new Employee(
            Guid.NewGuid(),
            "EMP002",
            "Võ Thu Hà",
            null,
            null,
            null,
            new DateOnly(2019, 6, 20),
            "Hành chính",
            "Chuyên viên hành chính",
            EmployeeStatus.Inactive,
            terminationDate);

        viewModel.LoadEmployee(employee);

        EmployeeStatus status =
            Assert.Single(viewModel.StatusOptions);

        Assert.Equal(
            EmployeeStatus.Inactive,
            status);

        Assert.Equal(
            EmployeeStatus.Inactive,
            viewModel.SelectedStatus);
    }

    [Fact]
    public async Task LoadOrganizationOptionsAsync_IncludesAndSelectsCurrentInactiveDepartment()
    {
        var currentDepartment =
            new Department(
                Guid.NewGuid(),
                "OLD-HR",
                "Nhân sự cũ",
                false);

        var activeDepartment =
            new Department(
                Guid.NewGuid(),
                "IT",
                "Công nghệ thông tin");

        var otherInactiveDepartment =
            new Department(
                Guid.NewGuid(),
                "OLD-OTHER",
                "Phòng ban cũ khác",
                false);

        var currentPosition =
            new Position(
                Guid.NewGuid(),
                "SPEC",
                "Chuyên viên");

        var employee =
            new Employee(
                Guid.NewGuid(),
                "EMP-EDIT-001",
                "Nguyễn Văn An",
                null,
                null,
                null,
                new DateOnly(2024, 1, 1),
                currentDepartment.Name,
                currentPosition.Name,
                EmployeeStatus.Active,
                departmentId: currentDepartment.Id,
                positionId: currentPosition.Id);

        var employeeService =
            new StubEmployeeService();

        var departmentService =
            new StubDepartmentService(
                new[]
                {
                currentDepartment,
                activeDepartment,
                otherInactiveDepartment
                });

        var positionService =
            new StubPositionService(
                new[]
                {
                currentPosition
                });

        var viewModel =
            new EditEmployeeViewModel(
                employeeService,
                departmentService,
                positionService);

        viewModel.LoadEmployee(employee);

        await viewModel.LoadOrganizationOptionsAsync();

        Assert.Contains(
            viewModel.DepartmentOptions,
            department =>
                department.Id == activeDepartment.Id);

        Assert.Contains(
            viewModel.DepartmentOptions,
            department =>
                department.Id == currentDepartment.Id);

        Assert.DoesNotContain(
            viewModel.DepartmentOptions,
            department =>
                department.Id == otherInactiveDepartment.Id);

        Assert.NotNull(
            viewModel.SelectedDepartment);

        Assert.Equal(
            currentDepartment.Id,
            viewModel.SelectedDepartment.Id);
    }

    [Fact]
    public async Task LoadOrganizationOptionsAsync_IncludesAndSelectsCurrentInactivePosition()
    {
        var currentDepartment =
            new Department(
                Guid.NewGuid(),
                "HR",
                "Nhân sự");

        var currentPosition =
            new Position(
                Guid.NewGuid(),
                "OLD-SPEC",
                "Chuyên viên cũ",
                false);

        var activePosition =
            new Position(
                Guid.NewGuid(),
                "DEV",
                "Lập trình viên");

        var otherInactivePosition =
            new Position(
                Guid.NewGuid(),
                "OLD-OTHER",
                "Chức danh cũ khác",
                false);

        var employee =
            new Employee(
                Guid.NewGuid(),
                "EMP-EDIT-002",
                "Nguyễn Minh Anh",
                null,
                null,
                null,
                new DateOnly(2024, 1, 1),
                currentDepartment.Name,
                currentPosition.Name,
                EmployeeStatus.Active,
                departmentId: currentDepartment.Id,
                positionId: currentPosition.Id);

        var employeeService =
            new StubEmployeeService();

        var departmentService =
            new StubDepartmentService(
                new[]
                {
                currentDepartment
                });

        var positionService =
            new StubPositionService(
                new[]
                {
                currentPosition,
                activePosition,
                otherInactivePosition
                });

        var viewModel =
            new EditEmployeeViewModel(
                employeeService,
                departmentService,
                positionService);

        viewModel.LoadEmployee(employee);

        await viewModel.LoadOrganizationOptionsAsync();

        Assert.Contains(
            viewModel.PositionOptions,
            position =>
                position.Id == activePosition.Id);

        Assert.Contains(
            viewModel.PositionOptions,
            position =>
                position.Id == currentPosition.Id);

        Assert.DoesNotContain(
            viewModel.PositionOptions,
            position =>
                position.Id == otherInactivePosition.Id);

        Assert.NotNull(
            viewModel.SelectedPosition);

        Assert.Equal(
            currentPosition.Id,
            viewModel.SelectedPosition.Id);
    }
    private sealed class StubDepartmentService
    : IDepartmentService
    {
        private readonly IReadOnlyList<Department> _departments;

        public StubDepartmentService(
            IReadOnlyList<Department>? departments = null)
        {
            _departments =
                departments
                ?? Array.Empty<Department>();
        }

        public Task<IReadOnlyList<Department>> GetDepartmentsAsync(
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_departments);
        }

        public Task<DepartmentOperationResult> CreateDepartmentAsync(
            CreateDepartmentRequest request,
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<DepartmentOperationResult> UpdateDepartmentAsync(
            UpdateDepartmentRequest request,
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<DepartmentOperationResult> DeactivateDepartmentAsync(
            Guid departmentId,
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<DepartmentOperationResult> ReactivateDepartmentAsync(
            Guid departmentId,
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException();
    }

    private sealed class StubPositionService
        : IPositionService
    {
        private readonly IReadOnlyList<Position> _positions;

        public StubPositionService(
            IReadOnlyList<Position>? positions = null)
        {
            _positions =
                positions
                ?? Array.Empty<Position>();
        }

        public Task<IReadOnlyList<Position>> GetPositionsAsync(
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_positions);
        }

        public Task<PositionOperationResult> CreatePositionAsync(
            CreatePositionRequest request,
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<PositionOperationResult> UpdatePositionAsync(
            UpdatePositionRequest request,
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<PositionOperationResult> DeactivatePositionAsync(
            Guid positionId,
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<PositionOperationResult> ReactivatePositionAsync(
            Guid positionId,
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException();
    }

    private static EditEmployeeViewModel CreateViewModel(
    StubEmployeeService service)
    {
        return new EditEmployeeViewModel(
            service,
            new StubDepartmentService(),
            new StubPositionService());
    }
}
