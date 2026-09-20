namespace HrManagement.Application.Persistence.Backups;

public sealed record DatabaseBackupResult(
    string BackupFilePath,
    DateTimeOffset CreatedUtc,
    string? LatestAppliedMigration);
