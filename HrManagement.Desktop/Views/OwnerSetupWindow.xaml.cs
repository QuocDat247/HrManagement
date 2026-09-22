using HrManagement.Desktop.Branding;
using HrManagement.Desktop.ViewModels;
using System.Windows;

namespace HrManagement.Desktop.Views;

public partial class OwnerSetupWindow
    : Window
{
    private readonly OwnerSetupViewModel
        _viewModel;

    public OwnerSetupWindow(
        OwnerSetupViewModel viewModel,
        ApplicationBranding branding)
    {
        InitializeComponent();

        ArgumentNullException.ThrowIfNull(
            branding);

        Title =
            $"Thiết lập Chủ doanh nghiệp - "
            + branding.ProductDisplayName;

        BrandingFooterTextBlock.Text =
            $"{branding.ProductDisplayName} • "
            + branding.CustomerDisplayName;

        _viewModel =
            viewModel;

        DataContext =
            _viewModel;

        _viewModel.OwnerCreated +=
            OnOwnerCreated;

        _viewModel.RecoveryCodeReady +=
            OnRecoveryCodeReady;

        Closed +=
            OnWindowClosed;

        Loaded +=
            OnWindowLoaded;
    }

    private void OnRecoveryCodeReady(
        object? sender,
        EventArgs e)
    {
        PasswordBox.Clear();

        ConfirmPasswordBox.Clear();

        RecoveryCodeTextBox.Focus();

        RecoveryCodeTextBox.SelectAll();
    }

    private void OnWindowLoaded(
        object sender,
        RoutedEventArgs e)
    {
        UsernameTextBox.Focus();
    }

    private void CreateOwnerButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        var passwords =
            new OwnerSetupPasswords(
                PasswordBox.Password,
                ConfirmPasswordBox.Password);

        if (_viewModel.CreateOwnerCommand
            .CanExecute(
                passwords))
        {
            _viewModel.CreateOwnerCommand
                .Execute(
                    passwords);
        }
    }

    private void OnOwnerCreated(
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
        _viewModel.RecoveryCodeReady -=
            OnRecoveryCodeReady;

        _viewModel.OwnerCreated -=
            OnOwnerCreated;

        Closed -=
            OnWindowClosed;

        Loaded -=
            OnWindowLoaded;
    }
}
