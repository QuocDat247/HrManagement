using HrManagement.Desktop.Services.Departments;
using HrManagement.Desktop.ViewModels;
using HrManagement.Domain.Organization.Departments;
using System.Windows;

namespace HrManagement.Desktop.Views;

public partial class DepartmentEditorWindow
    : Window
{
    private readonly DepartmentEditorViewModel
        _viewModel;

    public DepartmentEditorDialogResult?
        Result
    {
        get;
        private set;
    }

    public DepartmentEditorWindow(
        DepartmentEditorViewModel viewModel)
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
        Department department)
    {
        Result = null;

        _viewModel.LoadForEdit(
            department);
    }

    private void ViewModel_ConfirmSucceeded(
        object? sender,
        EventArgs e)
    {
        Result =
            new DepartmentEditorDialogResult(
                _viewModel.Code,
                _viewModel.Name);

        DialogResult =
            true;
    }
}
