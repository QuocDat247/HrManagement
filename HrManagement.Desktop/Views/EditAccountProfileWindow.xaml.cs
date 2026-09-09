using HrManagement.Desktop.ViewModels;
using System.Windows;

namespace HrManagement.Desktop.Views;

public partial class EditAccountProfileWindow
    : Window
{
    private readonly EditAccountProfileViewModel
        _viewModel;

    private Guid _accountId;

    public EditAccountProfileWindow(
        EditAccountProfileViewModel viewModel)
    {
        InitializeComponent();

        _viewModel =
            viewModel;

        DataContext =
            _viewModel;

        _viewModel.AccountUpdated +=
            OnAccountUpdated;

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

        DisplayNameTextBox.Focus();

        DisplayNameTextBox.SelectAll();
    }

    private void OnAccountUpdated(
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
        _viewModel.AccountUpdated -=
            OnAccountUpdated;

        Loaded -=
            OnWindowLoaded;

        Closed -=
            OnWindowClosed;
    }
}
