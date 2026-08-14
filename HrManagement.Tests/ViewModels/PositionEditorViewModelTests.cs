using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HrManagement.Desktop.ViewModels;
using HrManagement.Domain.Organization.Positions;

namespace HrManagement.Tests.ViewModels;
public sealed class PositionEditorViewModelTests
{
    [Fact]
    public void LoadForAdd_ResetsEditor()
    {
        var viewModel =
            new PositionEditorViewModel
            {
                Code = "OLD",
                Name = "Old Position"
            };

        viewModel.LoadForAdd();

        Assert.Equal(
            "Thêm chức danh",
            viewModel.Title);

        Assert.Empty(
            viewModel.Code);

        Assert.Empty(
            viewModel.Name);

        Assert.Null(
            viewModel.ErrorMessage);
    }

    [Fact]
    public void LoadForEdit_LoadsPosition()
    {
        var position =
            new Position(
                Guid.NewGuid(),
                "DEV",
                "Lập trình viên");

        var viewModel =
            new PositionEditorViewModel();

        viewModel.LoadForEdit(
            position);

        Assert.Equal(
            "Sửa chức danh",
            viewModel.Title);

        Assert.Equal(
            "DEV",
            viewModel.Code);

        Assert.Equal(
            "Lập trình viên",
            viewModel.Name);
    }

    [Fact]
    public void ConfirmCommand_WhenCodeIsBlank_ReturnsValidationError()
    {
        var viewModel =
            new PositionEditorViewModel
            {
                Code = "   ",
                Name = "Lập trình viên"
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
            "Vui lòng nhập mã chức danh.",
            viewModel.ErrorMessage);
    }

    [Fact]
    public void ConfirmCommand_WithValidValues_NormalizesAndConfirms()
    {
        var viewModel =
            new PositionEditorViewModel
            {
                Code = " dev ",
                Name = " Lập trình viên "
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
            "DEV",
            viewModel.Code);

        Assert.Equal(
            "Lập trình viên",
            viewModel.Name);
    }
}
