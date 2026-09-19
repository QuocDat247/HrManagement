using HrManagement.Desktop.ViewModels;
using System.Windows;

namespace HrManagement.Desktop.Views;

public partial class OwnerRecoveryEnrollmentWindow
    : Window
{
    private readonly OwnerRecoveryEnrollmentViewModel
        _viewModel;

    public OwnerRecoveryEnrollmentWindow(
        OwnerRecoveryEnrollmentViewModel viewModel)
    {
        InitializeComponent();

        _viewModel =
            viewModel;

        DataContext =
            _viewModel;

        _viewModel.EnrollmentCompleted +=
            OnEnrollmentCompleted;

        Loaded +=
            OnWindowLoaded;

        Closed +=
            OnWindowClosed;
    }

    private void OnWindowLoaded(
        object sender,
        RoutedEventArgs e)
    {
        _viewModel.Prepare();

        RecoveryCodeTextBox.Focus();

        RecoveryCodeTextBox.SelectAll();
    }

    private void OnEnrollmentCompleted(
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
        RecoveryCodeTextBox.Clear();

        _viewModel.EnrollmentCompleted -=
            OnEnrollmentCompleted;

        Loaded -=
            OnWindowLoaded;

        Closed -=
            OnWindowClosed;
    }
}
