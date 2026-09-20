using HrManagement.Desktop.ViewModels;
using System.ComponentModel;
using System.Windows;

namespace HrManagement.Desktop.Views;

public partial class OwnerPasswordRecoveryWindow
    : Window
{
    private readonly OwnerPasswordRecoveryViewModel
        _viewModel;

    public OwnerPasswordRecoveryWindow(
        OwnerPasswordRecoveryViewModel viewModel)
    {
        InitializeComponent();

        _viewModel =
            viewModel;

        DataContext =
            _viewModel;

        _viewModel.RecoverySucceeded +=
            OnRecoverySucceeded;

        _viewModel.RecoveryFinished +=
            OnRecoveryFinished;

        Loaded +=
            OnWindowLoaded;

        Closing +=
            OnWindowClosing;

        Closed +=
            OnWindowClosed;
    }

    private void OnWindowLoaded(
        object sender,
        RoutedEventArgs e)
    {
        UsernameTextBox.Focus();
    }

    private void RecoverButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        var passwords =
            new OwnerPasswordRecoveryPasswords(
                RecoveryCodeBox.Password,
                NewPasswordBox.Password,
                ConfirmPasswordBox.Password);

        if (_viewModel
            .RecoverCommand
            .CanExecute(
                passwords))
        {
            _viewModel
                .RecoverCommand
                .Execute(
                    passwords);
        }
    }

    private void OnRecoverySucceeded(
        object? sender,
        EventArgs e)
    {
        RecoveryCodeBox.Clear();

        NewPasswordBox.Clear();

        ConfirmPasswordBox.Clear();

        NewRecoveryCodeTextBox.Focus();

        NewRecoveryCodeTextBox.SelectAll();
    }

    private void OnRecoveryFinished(
        object? sender,
        EventArgs e)
    {
        DialogResult =
            true;
    }

    private void OnWindowClosing(
        object? sender,
        CancelEventArgs e)
    {
        if (!_viewModel.IsRecoveryCompleted
            || _viewModel.IsCompletionAcknowledged)
        {
            return;
        }

        e.Cancel =
            true;

        MessageBox.Show(
            this,
            "Mật khẩu đã được thay đổi và mã khôi phục cũ đã hết hiệu lực.\n\n"
            + "Hãy lưu mã khôi phục mới rồi bấm \"Tôi đã lưu mã mới\".",
            "Lưu mã khôi phục mới",
            MessageBoxButton.OK,
            MessageBoxImage.Warning);

        NewRecoveryCodeTextBox.Focus();

        NewRecoveryCodeTextBox.SelectAll();
    }

    private void OnWindowClosed(
        object? sender,
        EventArgs e)
    {
        RecoveryCodeBox.Clear();

        NewPasswordBox.Clear();

        ConfirmPasswordBox.Clear();

        _viewModel.ClearSensitiveState();

        _viewModel.RecoverySucceeded -=
            OnRecoverySucceeded;

        _viewModel.RecoveryFinished -=
            OnRecoveryFinished;

        Loaded -=
            OnWindowLoaded;

        Closing -=
            OnWindowClosing;

        Closed -=
            OnWindowClosed;
    }
}
