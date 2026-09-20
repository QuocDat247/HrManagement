using HrManagement.Application.Persistence.Backups;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace HrManagement.Infrastructure.Persistence.Upgrades;

public sealed class DatabaseUpgradeSafetyService
{
    private const int MaxRetainedUpgradeBackups =
        5;

    private readonly IDbContextFactory<
        HrManagementDbContext>
        _dbContextFactory;

    private readonly IDatabaseBackupService
        _databaseBackupService;

    private readonly TimeProvider
        _timeProvider;

    public DatabaseUpgradeSafetyService(
        IDbContextFactory<HrManagementDbContext> dbContextFactory,
        IDatabaseBackupService databaseBackupService,
        TimeProvider timeProvider)
    {
        _dbContextFactory =
            dbContextFactory;

        _databaseBackupService =
            databaseBackupService;

        _timeProvider =
            timeProvider;
    }

    public async Task<string?>
        CreatePreMigrationBackupIfNeededAsync(
            CancellationToken cancellationToken = default)
    {
        cancellationToken
            .ThrowIfCancellationRequested();

        await using HrManagementDbContext dbContext =
            await _dbContextFactory
                .CreateDbContextAsync(
                    cancellationToken);

        if (dbContext.Database
                .GetDbConnection()
            is not SqliteConnection connection)
        {
            throw new InvalidOperationException(
                "Database hiện tại không sử dụng SQLite.");
        }

        if (string.IsNullOrWhiteSpace(
                connection.DataSource)
            || string.Equals(
                connection.DataSource,
                ":memory:",
                StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "Không thể xác định đường dẫn database hiện tại.");
        }

        string databasePath =
            Path.GetFullPath(
                connection.DataSource);

        /*
         * Fresh install:
         * database chưa tồn tại nên không có dữ liệu cũ
         * cần bảo vệ trước migration đầu tiên.
         *
         * Quan trọng: kiểm tra File.Exists trước khi gọi
         * GetPendingMigrationsAsync vì việc mở SQLite có
         * thể tự tạo file database.
         */
        if (!File.Exists(
                databasePath))
        {
            return null;
        }

        string[] pendingMigrations =
            (await dbContext.Database
                .GetPendingMigrationsAsync(
                    cancellationToken))
            .ToArray();

        if (pendingMigrations.Length ==
            0)
        {
            return null;
        }

        string? databaseDirectory =
            Path.GetDirectoryName(
                databasePath);

        if (string.IsNullOrWhiteSpace(
                databaseDirectory))
        {
            throw new InvalidOperationException(
                "Không thể xác định thư mục database.");
        }

        string backupDirectory =
            Path.Combine(
                databaseDirectory,
                "Backups",
                "Upgrades");

        Directory.CreateDirectory(
            backupDirectory);

        DateTimeOffset nowUtc =
            _timeProvider.GetUtcNow();

        string timestamp =
            nowUtc.ToString(
                "yyyyMMdd-HHmmss");

        string uniqueSuffix =
            Guid.NewGuid()
                .ToString(
                    "N")[..8];

        string backupPath =
            Path.Combine(
                backupDirectory,
                $"PreUpgrade-{timestamp}-{uniqueSuffix}.hrbackup");

        DatabaseBackupResult backupResult =
            await _databaseBackupService
                .CreateAsync(
                    backupPath,
                    cancellationToken);

        TryPruneOldUpgradeBackups(
            backupDirectory,
            backupResult.BackupFilePath);

        return backupResult.BackupFilePath;
    }

    private static void TryPruneOldUpgradeBackups(
        string backupDirectory,
        string currentBackupPath)
    {
        try
        {
            string currentFullPath =
                Path.GetFullPath(
                    currentBackupPath);

            FileInfo[] olderBackups =
                Directory
                    .EnumerateFiles(
                        backupDirectory,
                        "PreUpgrade-*.hrbackup",
                        SearchOption.TopDirectoryOnly)
                    .Select(
                        path =>
                            new FileInfo(
                                path))
                    .Where(
                        file =>
                            !string.Equals(
                                file.FullName,
                                currentFullPath,
                                StringComparison.OrdinalIgnoreCase))
                    .OrderByDescending(
                        file =>
                            file.LastWriteTimeUtc)
                    .ToArray();

            /*
             * Luôn giữ backup vừa tạo +
             * tối đa 4 backup upgrade cũ.
             */
            foreach (FileInfo backup
                     in olderBackups.Skip(
                         MaxRetainedUpgradeBackups - 1))
            {
                try
                {
                    backup.Delete();
                }
                catch
                {
                    // Retention cleanup is best effort.
                }
            }
        }
        catch
        {
            /*
             * Backup đã tạo thành công.
             * Cleanup file cũ không được phép chặn upgrade.
             */
        }
    }
}
