using HrManagement.Application.Employees;
using HrManagement.Desktop.ViewModels;
using HrManagement.Domain.Employees;

namespace HrManagement.Tests.Employees;

public sealed class EditEmployeeViewModelTests
{
    [Fact]
    public void LoadEmployee_PopulatesEditableFields()
    {
        var service = new StubEmployeeService();
        var viewModel = new EditEmployeeViewModel(service);

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

        Assert.Equal(employee.Department, viewModel.Department);
        Assert.Equal(employee.Position, viewModel.Position);
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

        var viewModel = new EditEmployeeViewModel(service);

        var employee = CreateEmployee();

        viewModel.LoadEmployee(employee);

        viewModel.FullName = "Nguyễn Văn An Updated";
        viewModel.Position = "Chuyên viên cao cấp";

        bool saveSucceeded = false;

        viewModel.SaveSucceeded += (_, _) =>
            saveSucceeded = true;

        await viewModel.SaveCommand.ExecuteAsync(null);

        Assert.True(saveSucceeded);
        Assert.Null(viewModel.ErrorMessage);

        Assert.NotNull(service.LastUpdateRequest);
        Assert.Equal(
            employee.Id,
            service.LastUpdateRequest.Id);

        Assert.Equal(
            "Nguyễn Văn An Updated",
            service.LastUpdateRequest.FullName);

        Assert.Equal(
            "Chuyên viên cao cấp",
            service.LastUpdateRequest.Position);
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

        var viewModel = new EditEmployeeViewModel(service);

        viewModel.LoadEmployee(CreateEmployee());

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
        var viewModel = new EditEmployeeViewModel(service);

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
        var viewModel = new EditEmployeeViewModel(service);

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
}
