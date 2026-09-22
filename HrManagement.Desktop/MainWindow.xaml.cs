using System;
using HrManagement.Desktop.Branding;
using HrManagement.Desktop.ViewModels;
using System.Windows;

namespace HrManagement.Desktop;

public partial class MainWindow : Window
{
    public MainWindow(
        MainViewModel viewModel,
        ApplicationBranding branding)
    {
        InitializeComponent();

        ArgumentNullException.ThrowIfNull(
            branding);

        Title =
            $"{branding.ProductDisplayName} - Quản trị nhân sự";

        ProductDisplayNameTextBlock.Text =
            branding.ProductDisplayName;

        CustomerDisplayNameTextBlock.Text =
            branding.CustomerDisplayName;

        DataContext =
            viewModel;

        viewModel.LogoutRequested +=
            OnLogoutRequested;

        Closed +=
            (_, _) =>
                viewModel.LogoutRequested -=
                    OnLogoutRequested;
    }

    private void OnLogoutRequested(
        object? sender,
        EventArgs e)
    {
        Close();
    }
}
