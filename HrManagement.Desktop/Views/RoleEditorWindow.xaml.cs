using HrManagement.Application.Authentication.Accounts;
using HrManagement.Desktop.ViewModels;
using System.Windows;

namespace HrManagement.Desktop.Views;

public partial class RoleEditorWindow
    : Window
{
    private readonly RoleEditorViewModel
        _viewModel;

    public RoleEditorWindow(
        RoleEditorViewModel viewModel)
    {
        InitializeComponent();

        _viewModel =
            viewModel;

        DataContext =
            _viewModel;

        _viewModel.RoleSaved +=
            OnRoleSaved;

        Loaded +=
            OnWindowLoaded;

        Closed +=
            OnWindowClosed;
    }

    public void LoadForCreate()
    {
        _viewModel.LoadForCreate();
    }

    public void LoadForEdit(
        AccountManagementRoleItem role)
    {
        _viewModel.LoadForEdit(
            role);
    }

    private void OnWindowLoaded(
        object sender,
        RoutedEventArgs e)
    {
        NameTextBox.Focus();

        NameTextBox.SelectAll();
    }

    private void OnRoleSaved(
        object? sender,
        EventArgs e)
    {
        DialogResult =
            true;
    }

    private void OnWindowClosed(
        object? sender,
        EventArgs e)
    {
        _viewModel.RoleSaved -=
            OnRoleSaved;

        Loaded -=
            OnWindowLoaded;

        Closed -=
            OnWindowClosed;
    }
}
