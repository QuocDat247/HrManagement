using HrManagement.Desktop.Services.Positions;
using HrManagement.Desktop.ViewModels;
using HrManagement.Domain.Organization.Positions;
using System.Windows;

namespace HrManagement.Desktop.Views;

public partial class PositionEditorWindow
    : Window
{
    private readonly PositionEditorViewModel
        _viewModel;

    public PositionEditorDialogResult?
        Result
    {
        get;
        private set;
    }

    public PositionEditorWindow(
        PositionEditorViewModel viewModel)
    {
        InitializeComponent();

        _viewModel =
            viewModel;

        DataContext =
            viewModel;

        _viewModel.ConfirmSucceeded +=
            ViewModel_ConfirmSucceeded;
    }

    public void LoadForAdd()
    {
        Result = null;

        _viewModel.LoadForAdd();
    }

    public void LoadForEdit(
        Position position)
    {
        Result = null;

        _viewModel.LoadForEdit(
            position);
    }

    private void ViewModel_ConfirmSucceeded(
        object? sender,
        EventArgs e)
    {
        Result =
            new PositionEditorDialogResult(
                _viewModel.Code,
                _viewModel.Name);

        DialogResult =
            true;
    }
}
