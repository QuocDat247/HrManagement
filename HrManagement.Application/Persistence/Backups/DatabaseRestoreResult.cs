namespace HrManagement.Application.Persistence.Backups;

public sealed record DatabaseRestoreResult(
    bool IsSuccessful,
    string? ErrorMessage = null,
    string? SafetyBackupFilePath = null,
    bool WasRolledBack = false,
    DatabaseBackupInfo? BackupInfo = null);
