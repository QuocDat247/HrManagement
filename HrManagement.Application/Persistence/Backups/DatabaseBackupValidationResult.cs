namespace HrManagement.Application.Persistence.Backups;

public sealed record DatabaseBackupValidationResult(
    bool IsValid,
    string? ErrorMessage = null,
    DatabaseBackupInfo? BackupInfo = null);
