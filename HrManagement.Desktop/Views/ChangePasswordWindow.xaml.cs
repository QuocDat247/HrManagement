using HrManagement.Desktop.ViewModels;
using System.Windows;

namespace HrManagement.Desktop.Views;

public partial class ChangePasswordWindow
    : Window
{
    private readonly ChangePasswordViewModel
        _viewModel;

    public ChangePasswordWindow(
        ChangePasswordViewModel viewModel)
    {
        InitializeComponent();

        _viewModel =
            viewModel;

        DataContext =
            _viewModel;

        _viewModel.PasswordChanged +=
            OnPasswordChanged;

        Loaded +=
            OnWindowLoaded;

        Closed +=
            OnWindowClosed;
    }

    private void OnWindowLoaded(
        object sender,
        RoutedEventArgs e)
    {
        CurrentPasswordBox.Focus();
    }

    private void ChangePasswordButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        var passwords =
            new ChangePasswordPasswords(
                CurrentPasswordBox.Password,
                NewPasswordBox.Password,
                ConfirmPasswordBox.Password);

        if (_viewModel
            .ChangePasswordCommand
            .CanExecute(
                passwords))
        {
            _viewModel
                .ChangePasswordCommand
                .Execute(
                    passwords);
        }
    }

    private void OnPasswordChanged(
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
        CurrentPasswordBox.Clear();

        NewPasswordBox.Clear();

        ConfirmPasswordBox.Clear();

        _viewModel.PasswordChanged -=
            OnPasswordChanged;

        Loaded -=
            OnWindowLoaded;

        Closed -=
            OnWindowClosed;
    }
}
