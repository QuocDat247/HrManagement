namespace HrManagement.Desktop.Services;

public sealed class WpfUserConfirmationService
    : IUserConfirmationService
{
    public bool Confirm(
        string title,
        string message)
    {
        System.Windows.MessageBoxResult result =
            System.Windows.MessageBox.Show(
                message,
                title,
                System.Windows.MessageBoxButton.YesNo,
                System.Windows.MessageBoxImage.Warning,
                System.Windows.MessageBoxResult.No);

        return result ==
            System.Windows.MessageBoxResult.Yes;
    }
}
