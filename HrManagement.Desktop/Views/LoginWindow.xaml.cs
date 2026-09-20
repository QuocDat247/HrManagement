using HrManagement.Desktop.Services.Authentication;
using HrManagement.Desktop.ViewModels;
using System.Windows;

namespace HrManagement.Desktop.Views;

public partial class LoginWindow : Window
{
    private readonly LoginViewModel _viewModel;

    private readonly IOwnerPasswordRecoveryDialogService
    _recoveryDialogService;

    public bool MustChangePassword =>
        _viewModel.MustChangePassword;

    public LoginWindow(
    LoginViewModel viewModel,
    IOwnerPasswordRecoveryDialogService recoveryDialogService)
    {
        InitializeComponent();

        _viewModel =
            viewModel;

        _recoveryDialogService =
            recoveryDialogService;

        DataContext =
            _viewModel;

        _viewModel.LoginSucceeded +=
            OnLoginSucceeded;

        Closed +=
            OnWindowClosed;

        Loaded +=
            OnWindowLoaded;
    }

    private void ForgotOwnerPasswordButton_Click(
    object sender,
    RoutedEventArgs e)
    {
        PasswordBox.Clear();

        bool recovered =
            _recoveryDialogService
                .ShowDialog(
                    this);

        PasswordBox.Clear();

        if (recovered)
        {
            PasswordBox.Focus();
        }
        else
        {
            UsernameTextBox.Focus();
        }
    }

    private void OnWindowLoaded(
    object sender,
    RoutedEventArgs e)
    {
        UsernameTextBox.Focus();
    }

    private void LoginButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        if (_viewModel.LoginCommand.CanExecute(PasswordBox.Password))
        {
            _viewModel.LoginCommand.Execute(PasswordBox.Password);
        }
    }

    private void OnLoginSucceeded(
        object? sender,
        EventArgs e)
    {
        DialogResult = true;
    }

    private void OnWindowClosed(
        object? sender,
        EventArgs e)
    {
        _viewModel.LoginSucceeded -= OnLoginSucceeded;

        PasswordBox.Clear();

        Closed -= OnWindowClosed;

        Loaded -= OnWindowLoaded;
    }
}
