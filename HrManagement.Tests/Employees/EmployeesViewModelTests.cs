using HrManagement.Application.Employees;
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
        var viewModel = new EmployeesViewModel(service);

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
        var viewModel = new EmployeesViewModel(service);

        await viewModel.LoadAsync();

        Assert.Empty(viewModel.Employees);
        Assert.False(viewModel.IsLoading);
        Assert.Equal(
            "Không thể tải danh sách nhân viên.",
            viewModel.ErrorMessage);
    }

    private sealed class StubEmployeeService : IEmployeeService
    {
        private readonly IReadOnlyList<Employee> _employees;

        public StubEmployeeService(IReadOnlyList<Employee> employees)
        {
            _employees = employees;
        }

        public Task<IReadOnlyList<Employee>> GetEmployeesAsync(
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_employees);
        }
    }

    private sealed class FailingEmployeeService : IEmployeeService
    {
        public Task<IReadOnlyList<Employee>> GetEmployeesAsync(
            CancellationToken cancellationToken = default)
        {
            return Task.FromException<IReadOnlyList<Employee>>(
                new InvalidOperationException("Test failure"));
        }
    }
}
