using HrManagement.Desktop.ViewModels;
using System.Windows;

namespace HrManagement.Desktop.Views;

public partial class CreateStandardAccountWindow
    : Window
{
    private readonly CreateStandardAccountViewModel
        _viewModel;

    public CreateStandardAccountWindow(
        CreateStandardAccountViewModel viewModel)
    {
        InitializeComponent();

        _viewModel =
            viewModel;

        DataContext =
            _viewModel;

        _viewModel.AccountCreated +=
            OnAccountCreated;

        Loaded +=
            OnWindowLoaded;

        Closed +=
            OnWindowClosed;
    }

    private async void OnWindowLoaded(
        object sender,
        RoutedEventArgs e)
    {
        await _viewModel.LoadAsync();

        UsernameTextBox.Focus();
    }

    private void CreateAccountButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        var passwords =
            new CreateStandardAccountPasswords(
                PasswordBox.Password,
                ConfirmPasswordBox.Password);

        if (_viewModel
            .CreateAccountCommand
            .CanExecute(
                passwords))
        {
            _viewModel
                .CreateAccountCommand
                .Execute(
                    passwords);
        }
    }

    private void OnAccountCreated(
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
        PasswordBox.Clear();

        ConfirmPasswordBox.Clear();

        _viewModel.AccountCreated -=
            OnAccountCreated;

        Loaded -=
            OnWindowLoaded;

        Closed -=
            OnWindowClosed;
    }
}
