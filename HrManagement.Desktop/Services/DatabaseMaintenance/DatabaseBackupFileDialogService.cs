using Microsoft.Win32;

namespace HrManagement.Desktop.Services.DatabaseMaintenance;

public sealed class DatabaseBackupFileDialogService
    : IDatabaseBackupFileDialogService
{
    public string? SelectBackupDestination()
    {
        var dialog =
            new SaveFileDialog
            {
                Title =
                    "Lưu bản sao lưu HR Management",

                Filter =
                    "HR Management backup (*.hrbackup)|*.hrbackup",

                DefaultExt =
                    ".hrbackup",

                AddExtension =
                    true,

                OverwritePrompt =
                    true,

                FileName =
                    $"HrManagement-Backup-{DateTime.Now:yyyyMMdd-HHmmss}.hrbackup"
            };

        bool? result =
            dialog.ShowDialog(
                System.Windows.Application.Current.MainWindow);

        return result == true
            ? dialog.FileName
            : null;
    }

    public string? SelectBackupForRestore()
    {
        var dialog =
            new OpenFileDialog
            {
                Title =
                    "Chọn bản sao lưu HR Management",

                Filter =
                    "HR Management backup (*.hrbackup)|*.hrbackup",

                DefaultExt =
                    ".hrbackup",

                CheckFileExists =
                    true,

                Multiselect =
                    false
            };

        bool? result =
            dialog.ShowDialog(
                System.Windows.Application.Current.MainWindow);

        return result == true
            ? dialog.FileName
            : null;
    }
}
