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
        public Guid? RehireEmployeeId
        {
            get;
            private set;
        }

        public DateOnly? RehireDate
        {
            get;
            private set;
        }

        public EmployeeStatus? RehireStatus
        {
            get;
            private set;
        }

        public RehireEmployeeResult RehireResult
        {
            get;
            set;
        } =
            new(
                true,
                null);


        public Task<RehireEmployeeResult> RehireEmployeeAsync(
            Guid employeeId,
            DateOnly rehireDate,
            EmployeeStatus rehireStatus,
            CancellationToken cancellationToken = default)
        {
            RehireEmployeeId =
                employeeId;

            RehireDate =
                rehireDate;

            RehireStatus =
                rehireStatus;

            return Task.FromResult(
                RehireResult);
        }

        public Guid?
        CancelDeactivationEmployeeId
        {
            get;
            private set;
        }

        public EmployeeStatus?
            CancelDeactivationRestoredStatus
        {
            get;
            private set;
        }

        public CancelEmployeeDeactivationResult
            CancelDeactivationResult
        {
            get;
            set;
        } =
            new(
                true,
                null);

        public Task<CancelEmployeeDeactivationResult>
        CancelDeactivationAsync(
        Guid employeeId,
        EmployeeStatus restoredStatus,
        CancellationToken cancellationToken = default)
        {
            CancelDeactivationEmployeeId =
                employeeId;

            CancelDeactivationRestoredStatus =
                restoredStatus;

            return Task.FromResult(
                CancelDeactivationResult);
        }

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
        public Employee?
            EmployeePassedToOrganizationHistoryDialog
        {
            get;
            private set;
        }

        public void ShowOrganizationHistoryDialog(
            Employee employee)
        {
            EmployeePassedToOrganizationHistoryDialog =
                employee;
        }

        public Employee?
            EmployeePassedToEmploymentHistoryDialog
        {
            get;
            private set;
        }

        public bool TransferResultToReturn
        {
            get;
            set;
        }

        public Employee?
            EmployeePassedToTransferDialog
        {
            get;
            private set;
        }

        public bool ShowTransferEmployeeDialog(
            Employee employee)
        {
            EmployeePassedToTransferDialog =
                employee;

            return TransferResultToReturn;
        }

        public void ShowEmploymentHistoryDialog(
            Employee employee)
        {
            EmployeePassedToEmploymentHistoryDialog =
                employee;
        }

        public RehireEmployeeDialogResult?
        RehireResultToReturn
        {
            get;
            set;
        }

        public Employee?
            EmployeePassedToRehireDialog
        {
            get;
            private set;
        }

        public RehireEmployeeDialogResult?
            ShowRehireEmployeeDialog(
                Employee employee)
        {
            EmployeePassedToRehireDialog =
                employee;

            return RehireResultToReturn;
        }

        public EmployeeStatus?
        CancelDeactivationStatusToReturn
        {
            get;
            set;
        }

        public Employee?
            EmployeePassedToCancelDeactivationDialog
        {
            get;
            private set;
        }

        public EmployeeStatus?
        ShowCancelEmployeeDeactivationDialog(
        Employee employee)
        {
            EmployeePassedToCancelDeactivationDialog =
                employee;

            return CancelDeactivationStatusToReturn;
        }

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
        public void ShowOrganizationHistoryDialog(
            Employee employee)
        {
        }

        public bool ShowTransferEmployeeDialog(
            Employee employee)
        {
            return false;
        }
        public void ShowEmploymentHistoryDialog(
            Employee employee)
        {
        }

        public RehireEmployeeDialogResult?
        ShowRehireEmployeeDialog(
        Employee employee)
        {
            return null;
        }

        public EmployeeStatus?
        ShowCancelEmployeeDeactivationDialog(
        Employee employee)
        {
            return null;
        }
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
        public void ShowOrganizationHistoryDialog(
            Employee employee)
        {
        }
        public bool ShowTransferEmployeeDialog(
            Employee employee)
        {
            return false;
        }

        public void ShowEmploymentHistoryDialog(
        Employee employee)
        {
        }

        public RehireEmployeeDialogResult?
        ShowRehireEmployeeDialog(
        Employee employee)
        {
            return null;
        }

        public EmployeeStatus?
        ShowCancelEmployeeDeactivationDialog(
        Employee employee)
        {
            return null;
        }
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
        public void ShowOrganizationHistoryDialog(
            Employee employee)
        {
        }

        public bool ShowTransferEmployeeDialog(
            Employee employee)
        {
            return false;
        }

        public void ShowEmploymentHistoryDialog(
            Employee employee)
        {
        }

        public RehireEmployeeDialogResult?
            ShowRehireEmployeeDialog(
                Employee employee)
                {
                    return null;
                }

        public EmployeeStatus?
        ShowCancelEmployeeDeactivationDialog(
        Employee employee)
        {
            return null;
        }
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

    [Fact]
    public void CancelDeactivationCommand_CanExecuteOnlyForInactiveEmployeeWithTerminationDate()
    {
        var employeeService =
            new StubEmployeeService(
                Array.Empty<Employee>());

        var dialogService =
            new StubEmployeeDialogService();

        var viewModel =
            new EmployeesViewModel(
                employeeService,
                dialogService);

        Employee activeEmployee =
            CreateTestEmployee(
                EmployeeStatus.Active);

        Employee legacyInactiveEmployee =
            CreateTestEmployee(
                EmployeeStatus.Inactive);

        Employee terminatedEmployee =
            CreateTestEmployee(
                EmployeeStatus.Inactive,
                new DateOnly(2026, 6, 15));

        viewModel.SelectedEmployee =
            activeEmployee;

        Assert.False(
            viewModel.CancelDeactivationCommand
                .CanExecute(null));

        viewModel.SelectedEmployee =
            legacyInactiveEmployee;

        Assert.False(
            viewModel.CancelDeactivationCommand
                .CanExecute(null));

        viewModel.SelectedEmployee =
            terminatedEmployee;

        Assert.True(
            viewModel.CancelDeactivationCommand
                .CanExecute(null));
    }

    [Fact]
    public async Task CancelDeactivationCommand_WhenConfirmed_CallsServiceWithSelectedStatus()
    {
        var employeeService =
            new StubEmployeeService(
                Array.Empty<Employee>());

        var dialogService =
            new StubEmployeeDialogService
            {
                CancelDeactivationStatusToReturn =
                    EmployeeStatus.OnLeave
            };

        var viewModel =
            new EmployeesViewModel(
                employeeService,
                dialogService);

        Employee employee =
            CreateTestEmployee(
                EmployeeStatus.Inactive,
                new DateOnly(2026, 6, 15));

        viewModel.SelectedEmployee =
            employee;

        await viewModel
            .CancelDeactivationCommand
            .ExecuteAsync(null);

        Assert.Same(
            employee,
            dialogService
                .EmployeePassedToCancelDeactivationDialog);

        Assert.Equal(
            employee.Id,
            employeeService
                .CancelDeactivationEmployeeId);

        Assert.Equal(
            EmployeeStatus.OnLeave,
            employeeService
                .CancelDeactivationRestoredStatus);
    }

    [Fact]
    public async Task CancelDeactivationCommand_WhenDialogIsCancelled_DoesNotCallService()
    {
        var employeeService =
            new StubEmployeeService(
                Array.Empty<Employee>());

        var dialogService =
            new StubEmployeeDialogService
            {
                CancelDeactivationStatusToReturn =
                    null
            };

        var viewModel =
            new EmployeesViewModel(
                employeeService,
                dialogService);

        viewModel.SelectedEmployee =
            CreateTestEmployee(
                EmployeeStatus.Inactive,
                new DateOnly(2026, 6, 15));

        await viewModel
            .CancelDeactivationCommand
            .ExecuteAsync(null);

        Assert.Null(
            employeeService
                .CancelDeactivationEmployeeId);

        Assert.Null(
            employeeService
                .CancelDeactivationRestoredStatus);
    }

    private static Employee CreateTestEmployee(
    EmployeeStatus status,
    DateOnly? terminationDate = null)
    {
        return new Employee(
            Guid.NewGuid(),
            "EMP-TEST",
            "Nhân viên kiểm thử",
            null,
            null,
            null,
            new DateOnly(2024, 1, 1),
            "Nhân sự",
            "Chuyên viên",
            status,
            terminationDate);
    }

    [Fact]
    public void RehireEmployeeCommand_CanExecuteOnlyForInactiveEmployeeWithTerminationDate()
    {
        var employeeService =
            new StubEmployeeService(
                Array.Empty<Employee>());

        var dialogService =
            new StubEmployeeDialogService();

        var viewModel =
            new EmployeesViewModel(
                employeeService,
                dialogService);

        viewModel.SelectedEmployee =
            CreateTestEmployee(
                EmployeeStatus.Active);

        Assert.False(
            viewModel.RehireEmployeeCommand
                .CanExecute(null));

        viewModel.SelectedEmployee =
            CreateTestEmployee(
                EmployeeStatus.Inactive);

        Assert.False(
            viewModel.RehireEmployeeCommand
                .CanExecute(null));

        viewModel.SelectedEmployee =
            CreateTestEmployee(
                EmployeeStatus.Inactive,
                new DateOnly(2026, 6, 15));

        Assert.True(
            viewModel.RehireEmployeeCommand
                .CanExecute(null));
    }

    [Fact]
    public async Task RehireEmployeeCommand_WhenConfirmed_CallsServiceWithDialogValues()
    {
        var employeeService =
            new StubEmployeeService(
                Array.Empty<Employee>());

        DateOnly rehireDate =
            new(2026, 8, 1);

        var dialogService =
            new StubEmployeeDialogService
            {
                RehireResultToReturn =
                    new RehireEmployeeDialogResult(
                        rehireDate,
                        EmployeeStatus.OnLeave)
            };

        var viewModel =
            new EmployeesViewModel(
                employeeService,
                dialogService);

        Employee employee =
            CreateTestEmployee(
                EmployeeStatus.Inactive,
                new DateOnly(2026, 6, 15));

        viewModel.SelectedEmployee =
            employee;

        await viewModel
            .RehireEmployeeCommand
            .ExecuteAsync(null);

        Assert.Same(
            employee,
            dialogService
                .EmployeePassedToRehireDialog);

        Assert.Equal(
            employee.Id,
            employeeService.RehireEmployeeId);

        Assert.Equal(
            rehireDate,
            employeeService.RehireDate);

        Assert.Equal(
            EmployeeStatus.OnLeave,
            employeeService.RehireStatus);
    }

    [Fact]
    public async Task RehireEmployeeCommand_WhenDialogIsCancelled_DoesNotCallService()
    {
        var employeeService =
            new StubEmployeeService(
                Array.Empty<Employee>());

        var dialogService =
            new StubEmployeeDialogService
            {
                RehireResultToReturn = null
            };

        var viewModel =
            new EmployeesViewModel(
                employeeService,
                dialogService);

        viewModel.SelectedEmployee =
            CreateTestEmployee(
                EmployeeStatus.Inactive,
                new DateOnly(2026, 6, 15));

        await viewModel
            .RehireEmployeeCommand
            .ExecuteAsync(null);

        Assert.Null(
            employeeService.RehireEmployeeId);

        Assert.Null(
            employeeService.RehireDate);

        Assert.Null(
            employeeService.RehireStatus);
    }

    [Fact]
    public void ViewEmploymentHistoryCommand_CanExecuteOnlyWhenEmployeeIsSelected()
    {
        var employeeService =
            new StubEmployeeService(
                Array.Empty<Employee>());

        var dialogService =
            new StubEmployeeDialogService();

        var viewModel =
            new EmployeesViewModel(
                employeeService,
                dialogService);

        Assert.False(
            viewModel
                .ViewEmploymentHistoryCommand
                .CanExecute(null));

        viewModel.SelectedEmployee =
            CreateTestEmployee(
                EmployeeStatus.Active);

        Assert.True(
            viewModel
                .ViewEmploymentHistoryCommand
                .CanExecute(null));
    }

    [Fact]
    public void ViewEmploymentHistoryCommand_WhenExecuted_OpensDialogForSelectedEmployee()
    {
        var employeeService =
            new StubEmployeeService(
                Array.Empty<Employee>());

        var dialogService =
            new StubEmployeeDialogService();

        var viewModel =
            new EmployeesViewModel(
                employeeService,
                dialogService);

        Employee employee =
            CreateTestEmployee(
                EmployeeStatus.Inactive,
                new DateOnly(2026, 6, 15));

        viewModel.SelectedEmployee =
            employee;

        viewModel
            .ViewEmploymentHistoryCommand
            .Execute(null);

        Assert.Same(
            employee,
            dialogService
                .EmployeePassedToEmploymentHistoryDialog);
    }

    // Inactive không được transfer
    [Fact]
    public void TransferEmployeeCommand_WhenEmployeeIsInactive_CannotExecute()
    {
        var employee =
            new Employee(
                Guid.NewGuid(),
                "EMP-TRANSFER-VM-001",
                "Nhân viên đã nghỉ",
                null,
                null,
                null,
                new DateOnly(2024, 1, 1),
                "Nhân sự",
                "Chuyên viên",
                EmployeeStatus.Inactive,
                new DateOnly(2026, 7, 31));

        var service =
            new StubEmployeeService(
                [employee]);

        var dialogService =
            new StubEmployeeDialogService();

        var viewModel =
            new EmployeesViewModel(
                service,
                dialogService);

        viewModel.SelectedEmployee =
            employee;

        Assert.False(
            viewModel.TransferEmployeeCommand
                .CanExecute(null));
    }

    // Dialog save thành công → reload
    [Fact]
    public async Task TransferEmployeeCommand_WhenDialogSaves_ReloadsEmployees()
    {
        Guid employeeId =
            Guid.NewGuid();

        Guid sourceDepartmentId =
            Guid.NewGuid();

        Guid sourcePositionId =
            Guid.NewGuid();

        Guid targetDepartmentId =
            Guid.NewGuid();

        Guid targetPositionId =
            Guid.NewGuid();

        var originalEmployee =
            new Employee(
                employeeId,
                "EMP-TRANSFER-VM-002",
                "Nguyễn Văn An",
                null,
                null,
                null,
                new DateOnly(2024, 1, 1),
                "Phát triển phần mềm",
                "Kỹ sư phần mềm",
                EmployeeStatus.Active,
                departmentId:
                    sourceDepartmentId,
                positionId:
                    sourcePositionId);

        var transferredEmployee =
            new Employee(
                employeeId,
                "EMP-TRANSFER-VM-002",
                "Nguyễn Văn An",
                null,
                null,
                null,
                new DateOnly(2024, 1, 1),
                "Nghiên cứu và phát triển",
                "Trưởng nhóm kỹ thuật",
                EmployeeStatus.Active,
                departmentId:
                    targetDepartmentId,
                positionId:
                    targetPositionId);

        var service =
            new ReloadingEmployeeService(
                [originalEmployee],
                [transferredEmployee]);

        var dialogService =
            new StubEmployeeDialogService
            {
                TransferResultToReturn =
                    true
            };

        var viewModel =
            new EmployeesViewModel(
                service,
                dialogService);

        await viewModel.LoadAsync();

        viewModel.SelectedEmployee =
            viewModel.Employees[0];

        Assert.True(
            viewModel.TransferEmployeeCommand
                .CanExecute(null));

        await viewModel.TransferEmployeeCommand
            .ExecuteAsync(null);

        Assert.Equal(
            2,
            service.LoadCallCount);

        Employee employee =
            Assert.Single(
                viewModel.Employees);

        Assert.Equal(
            targetDepartmentId,
            employee.DepartmentId);

        Assert.Equal(
            targetPositionId,
            employee.PositionId);

        Assert.Equal(
            "Nghiên cứu và phát triển",
            employee.Department);

        Assert.Equal(
            "Trưởng nhóm kỹ thuật",
            employee.Position);

        Assert.Null(
            viewModel.SelectedEmployee);

        Assert.Same(
            originalEmployee,
            dialogService
                .EmployeePassedToTransferDialog);
    }

    // Hủy dialog → không reload
    [Fact]
    public async Task TransferEmployeeCommand_WhenDialogIsCancelled_DoesNotReload()
    {
        var employee =
            new Employee(
                Guid.NewGuid(),
                "EMP-TRANSFER-VM-003",
                "Nhân viên kiểm thử",
                null,
                null,
                null,
                new DateOnly(2024, 1, 1),
                "Phát triển phần mềm",
                "Kỹ sư phần mềm",
                EmployeeStatus.OnLeave,
                departmentId:
                    Guid.NewGuid(),
                positionId:
                    Guid.NewGuid());

        var service =
            new ReloadingEmployeeService(
                [employee],
                []);

        var dialogService =
            new StubEmployeeDialogService
            {
                TransferResultToReturn =
                    false
            };

        var viewModel =
            new EmployeesViewModel(
                service,
                dialogService);

        await viewModel.LoadAsync();

        viewModel.SelectedEmployee =
            viewModel.Employees[0];

        Assert.True(
            viewModel.TransferEmployeeCommand
                .CanExecute(null));

        await viewModel.TransferEmployeeCommand
            .ExecuteAsync(null);

        Assert.Equal(
            1,
            service.LoadCallCount);

        Assert.Single(
            viewModel.Employees);

        Assert.Same(
            employee,
            viewModel.SelectedEmployee);

        Assert.Same(
            employee,
            dialogService
                .EmployeePassedToTransferDialog);
    }

    // Không chọn nhân viên → command disabled
    [Fact]
    public void ViewOrganizationHistoryCommand_WhenNoEmployeeSelected_CannotExecute()
    {
        var service =
            new StubEmployeeService(
                []);

        var dialogService =
            new StubEmployeeDialogService();

        var viewModel =
            new EmployeesViewModel(
                service,
                dialogService);

        Assert.Null(
            viewModel.SelectedEmployee);

        Assert.False(
            viewModel.ViewOrganizationHistoryCommand
                .CanExecute(null));
    }

    // Có selection → mở đúng employee
    [Fact]
    public void ViewOrganizationHistoryCommand_WhenEmployeeSelected_OpensHistoryDialog()
    {
        var employee =
            new Employee(
                Guid.NewGuid(),
                "EMP-HISTORY-CMD-001",
                "Trần Thị Bình",
                null,
                null,
                null,
                new DateOnly(2024, 2, 1),
                "Nhân sự",
                "Chuyên viên",
                EmployeeStatus.Inactive,
                new DateOnly(2026, 7, 31),
                departmentId:
                    Guid.NewGuid(),
                positionId:
                    Guid.NewGuid());

        var service =
            new StubEmployeeService(
                [employee]);

        var dialogService =
            new StubEmployeeDialogService();

        var viewModel =
            new EmployeesViewModel(
                service,
                dialogService);

        viewModel.SelectedEmployee =
            employee;

        Assert.True(
            viewModel.ViewOrganizationHistoryCommand
                .CanExecute(null));

        viewModel.ViewOrganizationHistoryCommand
            .Execute(null);

        Assert.Same(
            employee,
            dialogService
                .EmployeePassedToOrganizationHistoryDialog);
    }
}
