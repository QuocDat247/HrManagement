namespace HrManagement.Application.Persistence.Backups;

public interface IDatabaseBackupValidationService
{
    Task<DatabaseBackupValidationResult> ValidateAsync(
        string backupFilePath,
        CancellationToken cancellationToken = default);
}
