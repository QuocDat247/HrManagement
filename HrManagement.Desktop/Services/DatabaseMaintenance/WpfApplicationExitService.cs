using System.Windows;

namespace HrManagement.Desktop.Services.DatabaseMaintenance;

public sealed class WpfApplicationExitService
    : IApplicationExitService
{
    public void Shutdown()
    {
        System.Windows.Application.Current.Shutdown(
            0);
    }
}
