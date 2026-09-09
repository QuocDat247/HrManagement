using HrManagement.Application.Authentication.Accounts;
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

    public bool ShowCreateRoleDialog()
    {
        RoleEditorWindow window =
            CreateRoleEditorWindow();

        window.LoadForCreate();

        return window.ShowDialog() ==
            true;
    }

    public bool ShowEditRoleDialog(
        AccountManagementRoleItem role)
    {
        ArgumentNullException.ThrowIfNull(
            role);

        RoleEditorWindow window =
            CreateRoleEditorWindow();

        window.LoadForEdit(
            role);

        return window.ShowDialog() ==
            true;
    }

    public bool ShowManageRolePermissionsDialog(
        Guid roleId)
    {
        if (roleId == Guid.Empty)
        {
            throw new ArgumentException(
                "Mã vai trò không hợp lệ.",
                nameof(roleId));
        }

        RolePermissionEditorWindow window =
            _serviceProvider
                .GetRequiredService<
                    RolePermissionEditorWindow>();

        window.Owner =
            System.Windows.Application
                .Current
                .MainWindow;

        window.LoadRole(
            roleId);

        return window.ShowDialog() ==
            true;
    }

    private RoleEditorWindow
        CreateRoleEditorWindow()
    {
        RoleEditorWindow window =
            _serviceProvider
                .GetRequiredService<
                    RoleEditorWindow>();

        window.Owner =
            System.Windows.Application
                .Current
                .MainWindow;

        return window;
    }
}
