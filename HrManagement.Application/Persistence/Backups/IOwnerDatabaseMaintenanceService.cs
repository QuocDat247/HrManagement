namespace HrManagement.Application.Persistence.Backups;

public interface IOwnerDatabaseMaintenanceService
{
    Task<bool> CanManageAsync(
        CancellationToken cancellationToken = default);

    Task<DatabaseBackupResult> CreateBackupAsync(
        string destinationFilePath,
        CancellationToken cancellationToken = default);

    Task<DatabaseRestoreResult> RestoreAsync(
        string backupFilePath,
        CancellationToken cancellationToken = default);
}
