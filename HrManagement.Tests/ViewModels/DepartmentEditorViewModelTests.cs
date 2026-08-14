using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HrManagement.Desktop.ViewModels;
using HrManagement.Domain.Organization.Departments;

namespace HrManagement.Tests.ViewModels;
public sealed class DepartmentEditorViewModelTests
{
    [Fact]
    public void LoadForAdd_ResetsEditor()
    {
        var viewModel =
            new DepartmentEditorViewModel();

        viewModel.Code =
            "OLD";

        viewModel.Name =
            "Old Department";

        viewModel.LoadForAdd();

        Assert.Equal(
            "Thêm phòng ban",
            viewModel.Title);

        Assert.Empty(
            viewModel.Code);

        Assert.Empty(
            viewModel.Name);

        Assert.Null(
            viewModel.ErrorMessage);
    }

    [Fact]
    public void LoadForEdit_LoadsDepartment()
    {
        var department =
            new Department(
                Guid.NewGuid(),
                "IT",
                "Công nghệ thông tin");

        var viewModel =
            new DepartmentEditorViewModel();

        viewModel.LoadForEdit(
            department);

        Assert.Equal(
            "Sửa phòng ban",
            viewModel.Title);

        Assert.Equal(
            "IT",
            viewModel.Code);

        Assert.Equal(
            "Công nghệ thông tin",
            viewModel.Name);
    }

    [Fact]
    public void ConfirmCommand_WhenCodeIsBlank_ReturnsValidationError()
    {
        var viewModel =
            new DepartmentEditorViewModel
            {
                Code = "   ",
                Name = "Nhân sự"
            };

        bool confirmed =
            false;

        viewModel.ConfirmSucceeded +=
            (_, _) => confirmed = true;

        viewModel.ConfirmCommand
            .Execute(null);

        Assert.False(
            confirmed);

        Assert.Equal(
            "Vui lòng nhập mã phòng ban.",
            viewModel.ErrorMessage);
    }

    [Fact]
    public void ConfirmCommand_WithValidValues_NormalizesAndConfirms()
    {
        var viewModel =
            new DepartmentEditorViewModel
            {
                Code = " hr ",
                Name = " Phòng Nhân sự "
            };

        bool confirmed =
            false;

        viewModel.ConfirmSucceeded +=
            (_, _) => confirmed = true;

        viewModel.ConfirmCommand
            .Execute(null);

        Assert.True(
            confirmed);

        Assert.Null(
            viewModel.ErrorMessage);

        Assert.Equal(
            "HR",
            viewModel.Code);

        Assert.Equal(
            "Phòng Nhân sự",
            viewModel.Name);
    }
}
