using HrManagement.Application.Employees;
using HrManagement.Desktop.ViewModels;
using HrManagement.Domain.Employees;
using HrManagement.Application.Organization.Departments;
using HrManagement.Application.Organization.Positions;
using HrManagement.Domain.Organization.Departments;
using HrManagement.Domain.Organization.Positions;

namespace HrManagement.Tests.Employees;
public sealed class AddEmployeeViewModelTests
{
    [Fact]
    public void StatusOptions_DoesNotContainInactive()
    {
        var service = new StubEmployeeService();

        var departmentService =
            new StubDepartmentService();

        var positionService =
            new StubPositionService();

        var viewModel =
            new AddEmployeeViewModel(
                service,
                departmentService,
                positionService);

        Assert.Contains(
            EmployeeStatus.Active,
            viewModel.StatusOptions);

        Assert.Contains(
            EmployeeStatus.OnLeave,
            viewModel.StatusOptions);

        Assert.DoesNotContain(
            EmployeeStatus.Inactive,
            viewModel.StatusOptions);
    }

    private sealed class StubEmployeeService : IEmployeeService
    {
        public CreateEmployeeRequest? LastCreateRequest
        {
            get;
            private set;
        }

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
            LastCreateRequest = request;

            return Task.FromResult(
                new CreateEmployeeResult(
                    IsSuccessful: true,
                    EmployeeId: Guid.NewGuid()));
        }

        public Task<UpdateEmployeeResult> UpdateEmployeeAsync(
            UpdateEmployeeRequest request,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(
                new UpdateEmployeeResult(
                    IsSuccessful: true));
        }

        public Task<DeactivateEmployeeResult> DeactivateEmployeeAsync(
            Guid employeeId,
            DateOnly? terminationDate = null,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(
                new DeactivateEmployeeResult(
                    IsSuccessful: true));
        }
    }

    [Fact]
    public async Task LoadOrganizationOptionsAsync_OnlyIncludesActiveDepartments()
    {
        var activeDepartment =
            new Department(
                Guid.NewGuid(),
                "HR",
                "Nhân sự");

        var inactiveDepartment =
            new Department(
                Guid.NewGuid(),
                "OLD",
                "Phòng ban cũ",
                false);

        var employeeService =
            new StubEmployeeService();

        var departmentService =
            new StubDepartmentService(
                new[]
                {
                activeDepartment,
                inactiveDepartment
                });

        var positionService =
            new StubPositionService();

        var viewModel =
            new AddEmployeeViewModel(
                employeeService,
                departmentService,
                positionService);

        await viewModel.LoadOrganizationOptionsAsync();

        Assert.Single(
            viewModel.DepartmentOptions);

        Assert.Equal(
            activeDepartment.Id,
            viewModel.DepartmentOptions[0].Id);
    }

    [Fact]
    public async Task LoadOrganizationOptionsAsync_OnlyIncludesActivePositions()
    {
        var activePosition =
            new Position(
                Guid.NewGuid(),
                "DEV",
                "Lập trình viên");

        var inactivePosition =
            new Position(
                Guid.NewGuid(),
                "OLD",
                "Chức danh cũ",
                false);

        var employeeService =
            new StubEmployeeService();

        var departmentService =
            new StubDepartmentService();

        var positionService =
            new StubPositionService(
                new[]
                {
                activePosition,
                inactivePosition
                });

        var viewModel =
            new AddEmployeeViewModel(
                employeeService,
                departmentService,
                positionService);

        await viewModel.LoadOrganizationOptionsAsync();

        Assert.Single(
            viewModel.PositionOptions);

        Assert.Equal(
            activePosition.Id,
            viewModel.PositionOptions[0].Id);
    }

    [Fact]
    public async Task SaveCommand_SendsSelectedOrganizationIds()
    {
        var department =
            new Department(
                Guid.NewGuid(),
                "HR",
                "Nhân sự");

        var position =
            new Position(
                Guid.NewGuid(),
                "DEV",
                "Lập trình viên");

        var employeeService =
            new StubEmployeeService();

        var departmentService =
            new StubDepartmentService();

        var positionService =
            new StubPositionService();

        var viewModel =
            new AddEmployeeViewModel(
                employeeService,
                departmentService,
                positionService);

        viewModel.EmployeeCode =
            "EMP-ORG-001";

        viewModel.FullName =
            "Nguyễn Minh Anh";

        viewModel.HireDate =
            new DateTime(2026, 8, 1);

        viewModel.SelectedStatus =
            EmployeeStatus.Active;

        viewModel.SelectedDepartment =
            department;

        viewModel.SelectedPosition =
            position;

        await viewModel.SaveCommand.ExecuteAsync(null);

        Assert.NotNull(
            employeeService.LastCreateRequest);

        Assert.Equal(
            department.Id,
            employeeService.LastCreateRequest.DepartmentId);

        Assert.Equal(
            position.Id,
            employeeService.LastCreateRequest.PositionId);
    }

    private sealed class StubDepartmentService
    : IDepartmentService
    {
        private readonly IReadOnlyList<Department>
            _departments;

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
            return Task.FromResult(
                _departments);
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
        private readonly IReadOnlyList<Position>
            _positions;

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
            return Task.FromResult(
                _positions);
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
}
