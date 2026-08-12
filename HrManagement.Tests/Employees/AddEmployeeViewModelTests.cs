using HrManagement.Application.Employees;
using HrManagement.Desktop.ViewModels;
using HrManagement.Domain.Employees;

namespace HrManagement.Tests.Employees;
public sealed class AddEmployeeViewModelTests
{
    [Fact]
    public void StatusOptions_DoesNotContainInactive()
    {
        var service = new StubEmployeeService();
        var viewModel = new AddEmployeeViewModel(service);

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
}
