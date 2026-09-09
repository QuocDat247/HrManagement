using HrManagement.Desktop.Views;
using Microsoft.Extensions.DependencyInjection;
using System.Windows;

namespace HrManagement.Desktop.Services.Accounts;

public sealed class AccountManagementDialogService
    : IAccountManagementDialogService
{
    private readonly IServiceProvider
        _serviceProvider;

    public AccountManagementDialogService(
        IServiceProvider serviceProvider)
    {
        _serviceProvider =
            serviceProvider;
    }

    public bool ShowCreateAccountDialog()
    {
        CreateStandardAccountWindow window =
            _serviceProvider
                .GetRequiredService<
                    CreateStandardAccountWindow>();

        window.Owner =
            System.Windows.Application
                .Current
                .MainWindow;

        return window.ShowDialog() ==
            true;
    }

    public bool ShowEditAccountDialog(
        Guid accountId)
    {
        EditAccountProfileWindow window =
            _serviceProvider
                .GetRequiredService<
                    EditAccountProfileWindow>();

        window.Owner =
            System.Windows.Application
                .Current
                .MainWindow;

        window.LoadAccount(
            accountId);

        return window.ShowDialog() ==
            true;
    }
}
