namespace HrManagement.Application.Persistence.Backups;

public interface IDatabaseRestoreService
{
    Task<DatabaseRestoreResult> RestoreAsync(
        string backupFilePath,
        CancellationToken cancellationToken = default);
}
