using HrManagement.Desktop.ViewModels;
using System.Windows;

namespace HrManagement.Desktop.Views;

public partial class RolePermissionEditorWindow
    : Window
{
    private readonly RolePermissionEditorViewModel
        _viewModel;

    private Guid _roleId;

    public RolePermissionEditorWindow(
        RolePermissionEditorViewModel viewModel)
    {
        InitializeComponent();

        _viewModel =
            viewModel;

        DataContext =
            _viewModel;

        _viewModel.PermissionsSaved +=
            OnPermissionsSaved;

        Loaded +=
            OnWindowLoaded;

        Closed +=
            OnWindowClosed;
    }

    public void LoadRole(
        Guid roleId)
    {
        if (roleId == Guid.Empty)
        {
            throw new ArgumentException(
                "Mã vai trò không hợp lệ.",
                nameof(roleId));
        }

        _roleId =
            roleId;
    }

    private async void OnWindowLoaded(
        object sender,
        RoutedEventArgs e)
    {
        await _viewModel.LoadAsync(
            _roleId);
    }

    private void OnPermissionsSaved(
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
        _viewModel.PermissionsSaved -=
            OnPermissionsSaved;

        Loaded -=
            OnWindowLoaded;

        Closed -=
            OnWindowClosed;
    }
}
