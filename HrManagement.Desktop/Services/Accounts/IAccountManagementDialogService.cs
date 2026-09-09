namespace HrManagement.Desktop.Services.Accounts;

public interface IAccountManagementDialogService
{
    bool ShowCreateAccountDialog();

    bool ShowEditAccountDialog(
        Guid accountId);
}
