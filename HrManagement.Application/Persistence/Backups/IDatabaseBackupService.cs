namespace HrManagement.Application.Persistence.Backups;

public interface IDatabaseBackupService
{
    Task<DatabaseBackupResult> CreateAsync(
        string destinationFilePath,
        CancellationToken cancellationToken = default);
}
