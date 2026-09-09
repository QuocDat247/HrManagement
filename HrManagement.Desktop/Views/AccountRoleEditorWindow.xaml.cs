using HrManagement.Desktop.ViewModels;
using System.Windows;

namespace HrManagement.Desktop.Views;

public partial class AccountRoleEditorWindow
    : Window
{
    private readonly AccountRoleEditorViewModel
        _viewModel;

    private Guid _accountId;

    public AccountRoleEditorWindow(
        AccountRoleEditorViewModel viewModel)
    {
        InitializeComponent();

        _viewModel =
            viewModel;

        DataContext =
            _viewModel;

        _viewModel.RolesSaved +=
            OnRolesSaved;

        Loaded +=
            OnWindowLoaded;

        Closed +=
            OnWindowClosed;
    }

    public void LoadAccount(
        Guid accountId)
    {
        if (accountId == Guid.Empty)
        {
            throw new ArgumentException(
                "Mã tài khoản không hợp lệ.",
                nameof(accountId));
        }

        _accountId =
            accountId;
    }

    private async void OnWindowLoaded(
        object sender,
        RoutedEventArgs e)
    {
        await _viewModel.LoadAsync(
            _accountId);
    }

    private void OnRolesSaved(
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
        _viewModel.RolesSaved -=
            OnRolesSaved;

        Loaded -=
            OnWindowLoaded;

        Closed -=
            OnWindowClosed;
    }
}
