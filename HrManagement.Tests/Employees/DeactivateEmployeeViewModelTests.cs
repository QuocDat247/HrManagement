using HrManagement.Desktop.ViewModels;
using HrManagement.Domain.Employees;

namespace HrManagement.Tests.Employees;

public sealed class DeactivateEmployeeViewModelTests
{
    [Fact]
    public void ConfirmCommand_WithValidTerminationDate_RaisesConfirmSucceeded()
    {
        var viewModel =
            new DeactivateEmployeeViewModel();

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

        viewModel.TerminationDate =
            DateTime.Today;

        bool confirmed = false;

        viewModel.ConfirmSucceeded += (_, _) =>
            confirmed = true;

        viewModel.ConfirmCommand.Execute(null);

        Assert.True(confirmed);
        Assert.Null(viewModel.ErrorMessage);
    }
}
