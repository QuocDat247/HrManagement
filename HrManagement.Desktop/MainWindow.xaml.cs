using System;
using HrManagement.Desktop.ViewModels;
using System.Windows;

namespace HrManagement.Desktop;

public partial class MainWindow : Window
{
    public MainWindow(MainViewModel viewModel)
    {
        InitializeComponent();

        DataContext = viewModel;

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
