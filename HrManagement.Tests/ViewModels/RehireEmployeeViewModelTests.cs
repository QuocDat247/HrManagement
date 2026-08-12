using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HrManagement.Desktop.ViewModels;
using HrManagement.Domain.Employees;

namespace HrManagement.Tests.ViewModels;
public sealed class RehireEmployeeViewModelTests
{
    [Fact]
    public void LoadEmployee_WithInactiveEmployee_LoadsRehireInformation()
    {
        DateOnly terminationDate =
            DateOnly.FromDateTime(
                DateTime.Today)
            .AddDays(-10);

        var employee =
            new Employee(
                Guid.NewGuid(),
                "EMP-REHIRE-UI-001",
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
            new RehireEmployeeViewModel();

        viewModel.LoadEmployee(employee);

        Assert.Equal(
            employee.EmployeeCode,
            viewModel.EmployeeCode);

        Assert.Equal(
            employee.FullName,
            viewModel.FullName);

        Assert.Equal(
            terminationDate.ToDateTime(
                TimeOnly.MinValue),
            viewModel.TerminationDate);

        Assert.Equal(
            DateTime.Today,
            viewModel.RehireDate.Date);

        Assert.Equal(
            EmployeeStatus.Active,
            viewModel.SelectedStatusOption.Status);
    }

    [Fact]
    public void ConfirmCommand_WithValidRehireDate_RaisesConfirmSucceeded()
    {
        DateOnly terminationDate =
            DateOnly.FromDateTime(
                DateTime.Today)
            .AddDays(-10);

        var employee =
            new Employee(
                Guid.NewGuid(),
                "EMP-REHIRE-UI-002",
                "Lê Minh Châu",
                null,
                null,
                null,
                new DateOnly(2024, 1, 1),
                "CNTT",
                "Lập trình viên",
                EmployeeStatus.Inactive,
                terminationDate);

        var viewModel =
            new RehireEmployeeViewModel();

        viewModel.LoadEmployee(employee);

        bool confirmed = false;

        viewModel.ConfirmSucceeded +=
            (_, _) => confirmed = true;

        viewModel.ConfirmCommand.Execute(null);

        Assert.True(confirmed);
        Assert.Null(viewModel.ErrorMessage);
    }

    [Fact]
    public void ConfirmCommand_WhenRehireDateIsSameAsTerminationDate_ReturnsValidationError()
    {
        DateOnly terminationDate =
            DateOnly.FromDateTime(
                DateTime.Today)
            .AddDays(-5);

        var employee =
            new Employee(
                Guid.NewGuid(),
                "EMP-REHIRE-UI-003",
                "Nhân viên kiểm thử",
                null,
                null,
                null,
                new DateOnly(2024, 1, 1),
                "Nhân sự",
                "Chuyên viên",
                EmployeeStatus.Inactive,
                terminationDate);

        var viewModel =
            new RehireEmployeeViewModel();

        viewModel.LoadEmployee(employee);

        viewModel.RehireDate =
            terminationDate.ToDateTime(
                TimeOnly.MinValue);

        bool confirmed = false;

        viewModel.ConfirmSucceeded +=
            (_, _) => confirmed = true;

        viewModel.ConfirmCommand.Execute(null);

        Assert.False(confirmed);

        Assert.Equal(
            "Ngày tái tuyển dụng phải sau ngày nghỉ việc gần nhất.",
            viewModel.ErrorMessage);
    }

    [Fact]
    public void ConfirmCommand_WhenRehireDateIsInFuture_ReturnsValidationError()
    {
        DateOnly terminationDate =
            DateOnly.FromDateTime(
                DateTime.Today)
            .AddDays(-10);

        var employee =
            new Employee(
                Guid.NewGuid(),
                "EMP-REHIRE-UI-004",
                "Nhân viên kiểm thử",
                null,
                null,
                null,
                new DateOnly(2024, 1, 1),
                "Nhân sự",
                "Chuyên viên",
                EmployeeStatus.Inactive,
                terminationDate);

        var viewModel =
            new RehireEmployeeViewModel();

        viewModel.LoadEmployee(employee);

        viewModel.RehireDate =
            DateTime.Today.AddDays(1);

        bool confirmed = false;

        viewModel.ConfirmSucceeded +=
            (_, _) => confirmed = true;

        viewModel.ConfirmCommand.Execute(null);

        Assert.False(confirmed);

        Assert.Equal(
            "Ngày tái tuyển dụng không thể ở tương lai.",
            viewModel.ErrorMessage);
    }
}
