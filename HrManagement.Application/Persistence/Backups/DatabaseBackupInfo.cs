namespace HrManagement.Application.Persistence.Backups;

public sealed record DatabaseBackupInfo(
    DateTimeOffset CreatedUtc,
    string DataMode,
    string? LatestAppliedMigration);
