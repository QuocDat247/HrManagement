using HrManagement.Application.Employees;
using HrManagement.Desktop.Services;
using HrManagement.Desktop.ViewModels;
using HrManagement.Domain.Employees;

namespace HrManagement.Tests.Employees;

public sealed class EmployeesViewModelTests
{
    [Fact]
    public async Task LoadAsync_WhenServiceSucceeds_PopulatesEmployees()
    {
        IReadOnlyList<Employee> employees =
        [
            new Employee(
                Guid.NewGuid(),
                "EMP001",
                "Nguyễn Văn An",
                "an@example.com",
                "0901000001",
                new DateOnly(1995, 5, 20),
                new DateOnly(2022, 3, 1),
                "Nhân sự",
                "Chuyên viên nhân sự",
                EmployeeStatus.Active)
        ];

        var service = new StubEmployeeService(employees);
        var viewModel = new EmployeesViewModel(
            service,
            new StubEmployeeDialogService());

        await viewModel.LoadAsync();

        Assert.Single(viewModel.Employees);
        Assert.Equal("EMP001", viewModel.Employees[0].EmployeeCode);
        Assert.False(viewModel.IsLoading);
        Assert.Null(viewModel.ErrorMessage);
    }

    [Fact]
    public async Task LoadAsync_WhenServiceFails_ClearsEmployeesAndSetsError()
    {
        var service = new FailingEmployeeService();
        var viewModel = new EmployeesViewModel(
            service,
            new StubEmployeeDialogService());

        await viewModel.LoadAsync();

        Assert.Empty(viewModel.Employees);
        Assert.False(viewModel.IsLoading);
        Assert.Equal(
            "Không thể tải danh sách nhân viên.",
            viewModel.ErrorMessage);
    }

    [Fact]
    public async Task ClearFiltersCommand_ClearsFiltersAndReloadsEmployees()
    {
        IReadOnlyList<Employee> employees =
        [
            new Employee(
            Guid.NewGuid(),
            "EMP001",
            "Nguyễn Văn An",
            null,
            null,
            null,
            new DateOnly(2022, 3, 1),
            "Nhân sự",
            "Chuyên viên nhân sự",
            EmployeeStatus.Active)
        ];

        var service = new StubEmployeeService(employees);
        var viewModel = new EmployeesViewModel(
            service,
            new StubEmployeeDialogService());

        viewModel.SearchText = "An";
        viewModel.SelectedStatusOption =
            viewModel.StatusOptions
                .First(option =>
                    option.Status == EmployeeStatus.Active);

        await viewModel.ClearFiltersCommand.ExecuteAsync(null);

        Assert.Null(viewModel.SearchText);
        Assert.Null(viewModel.SelectedStatusOption?.Status);
        Assert.Equal("Tất cả", viewModel.SelectedStatusOption?.DisplayName);
        Assert.Single(viewModel.Employees);
        Assert.False(viewModel.RequiresProfileCompletionOnly);
    }

    private sealed class StubEmployeeService : IEmployeeService
    {
        private readonly IReadOnlyList<Employee> _employees;

        public Task<DeactivateEmployeeResult> DeactivateEmployeeAsync(
            Guid employeeId,
            DateOnly? terminationDate = null,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(
                new DeactivateEmployeeResult(
                    IsSuccessful: true));
        }

        public Task<UpdateEmployeeResult> UpdateEmployeeAsync(
            UpdateEmployeeRequest request,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(
                new UpdateEmployeeResult(
                    IsSuccessful: true));
        }

        public StubEmployeeService(IReadOnlyList<Employee> employees)
        {
            _employees = employees;
        }

        public Task<IReadOnlyList<Employee>> GetEmployeesAsync(
            EmployeeFilter? filter = null,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_employees);
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
    }

    private sealed class FailingEmployeeService : IEmployeeService
    {
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

        public Task<IReadOnlyList<Employee>> GetEmployeesAsync(
            EmployeeFilter? filter = null,
            CancellationToken cancellationToken = default)
        {
            return Task.FromException<IReadOnlyList<Employee>>(
                new InvalidOperationException("Test failure"));
        }

        public Task<UpdateEmployeeResult> UpdateEmployeeAsync(
        UpdateEmployeeRequest request,
        CancellationToken cancellationToken = default)
        {
            return Task.FromResult(
                new UpdateEmployeeResult(
                    IsSuccessful: false,
                    ErrorMessage: "Test failure"));
        }

        public Task<CreateEmployeeResult> CreateEmployeeAsync(
            CreateEmployeeRequest request,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(
                new CreateEmployeeResult(
                    IsSuccessful: false,
                    ErrorMessage: "Test failure"));
        }
    }

    private sealed class StubEmployeeDialogService : IEmployeeDialogService
    {
        public DateOnly? ShowDeactivateEmployeeDialog(Employee employee)
        {
            return null;
        }

        public bool ShowAddEmployeeDialog() => false;
        public bool ShowEditEmployeeDialog(Employee employee) => false;
    }

    [Fact]
    public async Task AddEmployeeCommand_WhenDialogSaves_ReloadsEmployees()
    {
        IReadOnlyList<Employee> initialEmployees =
        [
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
            EmployeeStatus.Active)
        ];

        IReadOnlyList<Employee> reloadedEmployees =
        [
            .. initialEmployees,

        new Employee(
            Guid.NewGuid(),
            "EMP002",
            "Trần Thị Bình",
            null,
            null,
            null,
            new DateOnly(2023, 1, 10),
            "Kế toán",
            "Kế toán viên",
            EmployeeStatus.Active)
        ];

        var service =
            new ReloadingEmployeeService(
                initialEmployees,
                reloadedEmployees);

        var dialogService =
            new SuccessfulEmployeeDialogService();

        var viewModel =
            new EmployeesViewModel(
                service,
                dialogService);

        await viewModel.LoadAsync();

        Assert.Single(viewModel.Employees);

        await viewModel.AddEmployeeCommand.ExecuteAsync(null);

        Assert.Equal(2, viewModel.Employees.Count);
        Assert.Equal(2, service.LoadCallCount);
    }

    private sealed class ReloadingEmployeeService
    : IEmployeeService
    {
        private readonly IReadOnlyList<Employee> _firstResult;
        private readonly IReadOnlyList<Employee> _secondResult;

        public int LoadCallCount { get; private set; }

        public Task<DeactivateEmployeeResult> DeactivateEmployeeAsync(
            Guid employeeId,
            DateOnly? terminationDate = null,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(
                new DeactivateEmployeeResult(
                    IsSuccessful: true));
        }
        public Task<UpdateEmployeeResult> UpdateEmployeeAsync(
        UpdateEmployeeRequest request,
        CancellationToken cancellationToken = default)
        {
            return Task.FromResult(
                new UpdateEmployeeResult(
                    IsSuccessful: true));
        }

        public ReloadingEmployeeService(
            IReadOnlyList<Employee> firstResult,
            IReadOnlyList<Employee> secondResult)
        {
            _firstResult = firstResult;
            _secondResult = secondResult;
        }

        public Task<IReadOnlyList<Employee>> GetEmployeesAsync(
            EmployeeFilter? filter = null,
            CancellationToken cancellationToken = default)
        {
            LoadCallCount++;

            IReadOnlyList<Employee> result =
                LoadCallCount == 1
                    ? _firstResult
                    : _secondResult;

            return Task.FromResult(result);
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
    }

    private sealed class SuccessfulEmployeeDialogService : IEmployeeDialogService
    {
        public DateOnly? ShowDeactivateEmployeeDialog(Employee employee)
        {
            return null;
        }

        public bool ShowAddEmployeeDialog() => true;
        public bool ShowEditEmployeeDialog(Employee employee) => false;
    }

    [Fact]
    public async Task EditEmployeeCommand_WhenDialogSaves_ReloadsEmployees()
    {
        Guid employeeId = Guid.NewGuid();

        var originalEmployee = new Employee(
            employeeId,
            "EMP001",
            "Nguyễn Văn An",
            null,
            null,
            null,
            new DateOnly(2022, 3, 1),
            "Nhân sự",
            "Chuyên viên",
            EmployeeStatus.Active);

        var updatedEmployee = new Employee(
            employeeId,
            "EMP001",
            "Nguyễn Văn An Updated",
            null,
            null,
            null,
            new DateOnly(2022, 3, 1),
            "Nhân sự",
            "Chuyên viên cao cấp",
            EmployeeStatus.Active);

        var service = new ReloadingEmployeeService(
            [originalEmployee],
            [updatedEmployee]);

        var dialogService =
            new SuccessfulEditEmployeeDialogService();

        var viewModel = new EmployeesViewModel(
            service,
            dialogService);

        await viewModel.LoadAsync();

        viewModel.SelectedEmployee =
            viewModel.Employees[0];

        await viewModel.EditEmployeeCommand.ExecuteAsync(null);

        Assert.Equal(2, service.LoadCallCount);

        Employee employee =
            Assert.Single(viewModel.Employees);

        Assert.Equal(
            "Nguyễn Văn An Updated",
            employee.FullName);

        Assert.Equal(
            "Chuyên viên cao cấp",
            employee.Position);

        Assert.Null(viewModel.SelectedEmployee);

        Assert.Same(
            originalEmployee,
            dialogService.EmployeePassedToDialog);
    }

    private sealed class SuccessfulEditEmployeeDialogService : IEmployeeDialogService
    {
        public DateOnly? ShowDeactivateEmployeeDialog(Employee employee)
        {
            return null;
        }

        public Employee? EmployeePassedToDialog { get; private set; }

        public bool ShowAddEmployeeDialog() => false;

        public bool ShowEditEmployeeDialog(Employee employee)
        {
            EmployeePassedToDialog = employee;
            return true;
        }
    }

    [Fact]
    public async Task DeactivateEmployeeCommand_WhenConfirmed_DeactivatesAndReloads()
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

        var inactiveEmployee = new Employee(
            employee.Id,
            employee.EmployeeCode,
            employee.FullName,
            employee.Email,
            employee.PhoneNumber,
            employee.DateOfBirth,
            employee.HireDate,
            employee.Department,
            employee.Position,
            EmployeeStatus.Inactive);

        var service = new DeactivateEmployeeServiceStub(
            [employee],
            [inactiveEmployee]);

        DateOnly terminationDate =
    new DateOnly(2026, 8, 1);

        var dialogService =
            new DeactivateEmployeeDialogServiceStub(
                terminationDate);

        var viewModel =
            new EmployeesViewModel(
                service,
                dialogService);

        await viewModel.LoadAsync();

        viewModel.SelectedEmployee =
            viewModel.Employees[0];

        await viewModel
            .DeactivateEmployeeCommand
            .ExecuteAsync(null);

        Assert.Equal(
            employee.Id,
            service.DeactivatedEmployeeId);

        Assert.Equal(
            terminationDate,
            service.TerminationDate);

        Assert.Equal(
            2,
            service.LoadCallCount);

        Employee result =
            Assert.Single(viewModel.Employees);

        Assert.Equal(
            EmployeeStatus.Inactive,
            result.Status);

        Assert.Null(
            viewModel.SelectedEmployee);

        Assert.Same(
            employee,
            dialogService.EmployeePassedToDialog);
    }

    [Fact]
    public async Task DeactivateEmployeeCommand_WhenNotConfirmed_DoesNothing()
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

        var service = new DeactivateEmployeeServiceStub(
            [employee],
            [employee]);

        var dialogService =
            new DeactivateEmployeeDialogServiceStub(
                terminationDate: null);

        var viewModel =
            new EmployeesViewModel(
                service,
                dialogService);

        await viewModel.LoadAsync();

        viewModel.SelectedEmployee =
            viewModel.Employees[0];

        await viewModel
            .DeactivateEmployeeCommand
            .ExecuteAsync(null);

        Assert.Null(
            service.DeactivatedEmployeeId);

        Assert.Equal(
            1,
            service.LoadCallCount);

        Assert.NotNull(
            viewModel.SelectedEmployee);

        Assert.Equal(
            EmployeeStatus.Active,
            viewModel.SelectedEmployee.Status);
    }

    private sealed class DeactivateEmployeeDialogServiceStub : IEmployeeDialogService
    {
        private readonly DateOnly? _terminationDate;

        public Employee? EmployeePassedToDialog { get; private set; }

        public DeactivateEmployeeDialogServiceStub(DateOnly? terminationDate)
        {
            _terminationDate = terminationDate;
        }

        public bool ShowAddEmployeeDialog() => false;
        public bool ShowEditEmployeeDialog(Employee employee) => false;

        public DateOnly? ShowDeactivateEmployeeDialog(Employee employee)
        {
            EmployeePassedToDialog = employee;
            return _terminationDate;
        }
    }

    private sealed class DeactivateEmployeeServiceStub
    : IEmployeeService
    {
        private readonly IReadOnlyList<Employee> _firstResult;
        private readonly IReadOnlyList<Employee> _secondResult;

        public int LoadCallCount { get; private set; }

        public Guid? DeactivatedEmployeeId { get; private set; }

        public DateOnly? TerminationDate { get; private set; }

        public DeactivateEmployeeServiceStub(
            IReadOnlyList<Employee> firstResult,
            IReadOnlyList<Employee> secondResult)
        {
            _firstResult = firstResult;
            _secondResult = secondResult;
        }

        public Task<IReadOnlyList<Employee>> GetEmployeesAsync(
            EmployeeFilter? filter = null,
            CancellationToken cancellationToken = default)
        {
            LoadCallCount++;

            IReadOnlyList<Employee> result =
                LoadCallCount == 1
                    ? _firstResult
                    : _secondResult;

            return Task.FromResult(result);
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
            return Task.FromResult(
                new UpdateEmployeeResult(
                    IsSuccessful: true));
        }

        public Task<DeactivateEmployeeResult> DeactivateEmployeeAsync(
            Guid employeeId,
            DateOnly? terminationDate = null,
            CancellationToken cancellationToken = default)
        {
            DeactivatedEmployeeId = employeeId;
            TerminationDate = terminationDate;

            return Task.FromResult(
                new DeactivateEmployeeResult(
                    IsSuccessful: true));
        }
    }

    private sealed class CapturingEmployeeService
    : IEmployeeService
    {
        public EmployeeFilter? LastFilter { get; private set; }

        public Task<IReadOnlyList<Employee>> GetEmployeesAsync(
            EmployeeFilter? filter = null,
            CancellationToken cancellationToken = default)
        {
            LastFilter = filter;

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
    public async Task LoadAsync_WhenProfileCompletionFilterEnabled_PassesFilterToService()
    {
        var service =
            new CapturingEmployeeService();

        var dialogService =
            new StubEmployeeDialogService();

        var viewModel =
            new EmployeesViewModel(
                service,
                dialogService);

        viewModel.RequiresProfileCompletionOnly = true;

        await viewModel.LoadAsync();

        Assert.NotNull(service.LastFilter);

        Assert.True(
            service.LastFilter.RequiresProfileCompletionOnly);
    }

    [Fact]
    public async Task ShowProfileCompletionRequiredAsync_ResetsOtherFiltersAndLoadsProfileFilter()
    {
        var service =
            new CapturingEmployeeService();

        var dialogService =
            new StubEmployeeDialogService();

        var viewModel =
            new EmployeesViewModel(
                service,
                dialogService);

        viewModel.SearchText = "EMP001";

        viewModel.SelectedStatusOption =
            viewModel.StatusOptions
                .First(option =>
                    option.Status == EmployeeStatus.Active);

        viewModel.RequiresProfileCompletionOnly = false;

        await viewModel
            .ShowProfileCompletionRequiredAsync();

        Assert.Null(viewModel.SearchText);

        Assert.Null(
            viewModel.SelectedStatusOption?.Status);

        Assert.True(
            viewModel.RequiresProfileCompletionOnly);

        Assert.NotNull(service.LastFilter);

        Assert.True(
            service.LastFilter
                .RequiresProfileCompletionOnly);

        Assert.Null(
            service.LastFilter.SearchText);

        Assert.Null(
            service.LastFilter.Status);
    }
}
