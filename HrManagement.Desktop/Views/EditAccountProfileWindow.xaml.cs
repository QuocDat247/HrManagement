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

        _viewModel.PasswordReset +=
            OnPasswordReset;

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

    private void ResetPasswordButton_Click(
    object sender,
    RoutedEventArgs e)
    {
        var passwords =
            new ResetAccountPasswordPasswords(
                ResetPasswordBox.Password,
                ResetPasswordConfirmBox.Password);

        if (_viewModel
            .ResetPasswordCommand
            .CanExecute(passwords))
        {
            _viewModel
                .ResetPasswordCommand
                .Execute(passwords);
        }
    }

    private void OnPasswordReset(
        object? sender,
        EventArgs e)
    {
        ResetPasswordBox.Clear();

        ResetPasswordConfirmBox.Clear();
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
        ResetPasswordBox.Clear();

        ResetPasswordConfirmBox.Clear();

        _viewModel.PasswordReset -=
            OnPasswordReset;

        _viewModel.AccountUpdated -=
            OnAccountUpdated;

        Loaded -=
            OnWindowLoaded;

        Closed -=
            OnWindowClosed;
    }
}
