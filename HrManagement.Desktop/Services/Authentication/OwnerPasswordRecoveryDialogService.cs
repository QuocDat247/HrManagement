using HrManagement.Desktop.Views;
using Microsoft.Extensions.DependencyInjection;
using System.Windows;

namespace HrManagement.Desktop.Services.Authentication;

public sealed class OwnerPasswordRecoveryDialogService
    : IOwnerPasswordRecoveryDialogService
{
    private readonly IServiceProvider
        _serviceProvider;

    public OwnerPasswordRecoveryDialogService(
        IServiceProvider serviceProvider)
    {
        _serviceProvider =
            serviceProvider;
    }

    public bool ShowDialog(
        Window owner)
    {
        ArgumentNullException.ThrowIfNull(
            owner);

        OwnerPasswordRecoveryWindow window =
            _serviceProvider
                .GetRequiredService<
                    OwnerPasswordRecoveryWindow>();

        window.Owner =
            owner;

        return window.ShowDialog() ==
            true;
    }
}
