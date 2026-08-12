using HrManagement.Desktop.ViewModels;
using HrManagement.Domain.Employees;

namespace HrManagement.Tests.ViewModels;

public sealed class CancelEmployeeDeactivationViewModelTests
{
    [Fact]
    public void LoadEmployee_WithInactiveEmployee_LoadsRecordedTermination()
    {
        DateOnly terminationDate =
            new(2026, 6, 15);

        var employee =
            new Employee(
                Guid.NewGuid(),
                "EMP001",
                "Nguyễn Văn An",
                null,
                null,
                null,
                new DateOnly(2024, 1, 1),
                "Nhân sự",
                "Chuyên viên",
                EmployeeStatus.Inactive,
                terminationDate);

        var viewModel =
            new CancelEmployeeDeactivationViewModel();

        viewModel.LoadEmployee(employee);

        Assert.Equal(
            "EMP001",
            viewModel.EmployeeCode);

        Assert.Equal(
            "Nguyễn Văn An",
            viewModel.FullName);

        Assert.Equal(
            terminationDate.ToDateTime(
                TimeOnly.MinValue),
            viewModel.TerminationDate);

        Assert.Equal(
            EmployeeStatus.Active,
            viewModel.SelectedStatusOption.Status);
    }

    [Fact]
    public void StatusOptions_ContainsOnlyActiveAndOnLeave()
    {
        var viewModel =
            new CancelEmployeeDeactivationViewModel();

        Assert.Equal(
            2,
            viewModel.StatusOptions.Count);

        Assert.Contains(
            viewModel.StatusOptions,
            option =>
                option.Status ==
                    EmployeeStatus.Active);

        Assert.Contains(
            viewModel.StatusOptions,
            option =>
                option.Status ==
                    EmployeeStatus.OnLeave);

        Assert.DoesNotContain(
            viewModel.StatusOptions,
            option =>
                option.Status ==
                    EmployeeStatus.Inactive);
    }

    [Fact]
    public void ConfirmCommand_WithRecordedTermination_RaisesConfirmSucceeded()
    {
        var employee =
            new Employee(
                Guid.NewGuid(),
                "EMP002",
                "Lê Minh Châu",
                null,
                null,
                null,
                new DateOnly(2024, 1, 1),
                "CNTT",
                "Lập trình viên",
                EmployeeStatus.Inactive,
                new DateOnly(2026, 6, 15));

        var viewModel =
            new CancelEmployeeDeactivationViewModel();

        viewModel.LoadEmployee(employee);

        bool confirmed = false;

        viewModel.ConfirmSucceeded +=
            (_, _) => confirmed = true;

        viewModel.ConfirmCommand.Execute(null);

        Assert.True(confirmed);
        Assert.Null(viewModel.ErrorMessage);
    }
}
