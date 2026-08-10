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
        public Task<DeactivateEmployeeResult> DeactivateEmployeeAsync(
            Guid employeeId,
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
}
