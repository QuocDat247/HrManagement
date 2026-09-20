namespace HrManagement.Infrastructure.Persistence.Upgrades;

public sealed class DatabaseUpgradeCoordinator
{
    private readonly DatabaseUpgradeSafetyService
        _safetyService;

    private readonly IDatabaseMigrationExecutor
        _migrationExecutor;

    private readonly DatabaseUpgradeRollbackService
        _rollbackService;

    public DatabaseUpgradeCoordinator(
        DatabaseUpgradeSafetyService safetyService,
        IDatabaseMigrationExecutor migrationExecutor,
        DatabaseUpgradeRollbackService rollbackService)
    {
        _safetyService =
            safetyService;

        _migrationExecutor =
            migrationExecutor;

        _rollbackService =
            rollbackService;
    }

    public async Task<string?> UpgradeAsync(
        CancellationToken cancellationToken = default)
    {
        string? preMigrationBackupPath =
            await _safetyService
                .CreatePreMigrationBackupIfNeededAsync(
                    cancellationToken);

        try
        {
            await _migrationExecutor
                .MigrateAsync(
                    cancellationToken);

            return preMigrationBackupPath;
        }
        catch (OperationCanceledException)
            when (cancellationToken
                .IsCancellationRequested
                && !string.IsNullOrWhiteSpace(
                    preMigrationBackupPath))
        {
            await RollBackOrThrowAsync(
                preMigrationBackupPath!,
                new OperationCanceledException(
                    cancellationToken));

            throw;
        }
        catch (Exception migrationException)
            when (!string.IsNullOrWhiteSpace(
                preMigrationBackupPath))
        {
            await RollBackOrThrowAsync(
                preMigrationBackupPath!,
                migrationException);

            throw new InvalidOperationException(
                "Nâng cấp cơ sở dữ liệu thất bại. "
                + "Database trước nâng cấp đã được phục hồi an toàn. "
                + "Bản sao PreUpgrade vẫn được giữ tại:\n"
                + preMigrationBackupPath,
                migrationException);
        }
    }

    private async Task RollBackOrThrowAsync(
        string preMigrationBackupPath,
        Exception migrationException)
    {
        try
        {
            await _rollbackService
                .RestorePreUpgradeBackupAsync(
                    preMigrationBackupPath,
                    CancellationToken.None);
        }
        catch (Exception rollbackException)
        {
            throw new InvalidOperationException(
                "Nâng cấp cơ sở dữ liệu thất bại và rollback tự động "
                + "cũng thất bại. Không tiếp tục sử dụng database này. "
                + "PreUpgrade backup được giữ tại:\n"
                + preMigrationBackupPath,
                new AggregateException(
                    migrationException,
                    rollbackException));
        }
    }
}
