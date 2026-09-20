namespace HrManagement.Infrastructure.Persistence.Backups;

internal sealed record DatabaseBackupPackageMetadata(
    int FormatVersion,
    DateTimeOffset CreatedUtc,
    string DataMode,
    string? LatestAppliedMigration,
    string DatabaseEntryName,
    string DatabaseSha256);
