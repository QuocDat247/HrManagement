using System.Windows;

namespace HrManagement.Desktop.Services.Authentication;

public interface IOwnerPasswordRecoveryDialogService
{
    bool ShowDialog(
        Window owner);
}
