namespace HrManagement.Desktop.Services.DatabaseMaintenance;

public interface IDatabaseBackupFileDialogService
{
    string? SelectBackupDestination();

    string? SelectBackupForRestore();
}
