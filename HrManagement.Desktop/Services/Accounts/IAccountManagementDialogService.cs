using HrManagement.Application.Authentication.Accounts;

namespace HrManagement.Desktop.Services.Accounts;

public interface IAccountManagementDialogService
{
    bool ShowCreateAccountDialog();

    bool ShowEditAccountDialog(
        Guid accountId);

    bool ShowCreateRoleDialog();

    bool ShowEditRoleDialog(
        AccountManagementRoleItem role);

    bool ShowManageRolePermissionsDialog(
        Guid roleId);

    bool ShowManageAccountRolesDialog(
        Guid accountId);
}
