using HrManagement.Desktop.ViewModels;
using System.Windows;

namespace HrManagement.Desktop.Views;

public partial class LoginWindow : Window
{
    private readonly LoginViewModel _viewModel;

    public LoginWindow(LoginViewModel viewModel)
    {
        InitializeComponent();

        _viewModel = viewModel;
        DataContext = _viewModel;

        _viewModel.LoginSucceeded += OnLoginSucceeded;
        Closed += OnWindowClosed;

        Loaded += OnWindowLoaded;
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
        Closed -= OnWindowClosed;

        Loaded -= OnWindowLoaded;
    }
}
