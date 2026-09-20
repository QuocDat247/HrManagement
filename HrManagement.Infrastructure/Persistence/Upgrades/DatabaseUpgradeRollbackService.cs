using HrManagement.Application.Persistence.Backups;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using System.Data;
using System.IO.Compression;

namespace HrManagement.Infrastructure.Persistence.Upgrades;

public sealed class DatabaseUpgradeRollbackService
{
    private const string DatabaseEntryName =
        "database.sqlite";

    private readonly IDbContextFactory<
        HrManagementDbContext>
        _dbContextFactory;

    private readonly IDatabaseBackupValidationService
        _validationService;

    public DatabaseUpgradeRollbackService(
        IDbContextFactory<HrManagementDbContext> dbContextFactory,
        IDatabaseBackupValidationService validationService)
    {
        _dbContextFactory =
            dbContextFactory;

        _validationService =
            validationService;
    }

    public async Task RestorePreUpgradeBackupAsync(
        string backupFilePath,
        CancellationToken cancellationToken = default)
    {
        DatabaseBackupValidationResult validation =
            await _validationService
                .ValidateAsync(
                    backupFilePath,
                    cancellationToken);

        if (!validation.IsValid
            || validation.BackupInfo is null)
        {
            throw new InvalidDataException(
                "Pre-upgrade backup không hợp lệ.");
        }

        string temporaryDirectory =
            Path.Combine(
                Path.GetTempPath(),
                "HrManagement",
                "UpgradeRollback",
                Guid.NewGuid()
                    .ToString(
                        "N"));

        Directory.CreateDirectory(
            temporaryDirectory);

        string snapshotDatabasePath =
            Path.Combine(
                temporaryDirectory,
                DatabaseEntryName);

        try
        {
            await ExtractDatabaseAsync(
                backupFilePath,
                snapshotDatabasePath,
                cancellationToken);

            await RestoreSnapshotAsync(
                snapshotDatabasePath,
                cancellationToken);

            SqliteConnection.ClearAllPools();

            await VerifyRestoredDatabaseAsync(
                validation.BackupInfo,
                cancellationToken);
        }
        finally
        {
            TryDeleteDirectory(
                temporaryDirectory);
        }
    }

    private static async Task ExtractDatabaseAsync(
        string packagePath,
        string destinationDatabasePath,
        CancellationToken cancellationToken)
    {
        using ZipArchive archive =
            ZipFile.OpenRead(
                packagePath);

        ZipArchiveEntry? databaseEntry =
            archive.GetEntry(
                DatabaseEntryName);

        if (databaseEntry is null)
        {
            throw new InvalidDataException(
                "Pre-upgrade backup không chứa database.");
        }

        await using Stream source =
            databaseEntry.Open();

        await using var destination =
            new FileStream(
                destinationDatabasePath,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.None,
                bufferSize:
                    81920,
                FileOptions.Asynchronous);

        await source.CopyToAsync(
            destination,
            cancellationToken);
    }

    private async Task RestoreSnapshotAsync(
        string snapshotDatabasePath,
        CancellationToken cancellationToken)
    {
        SqliteConnection.ClearAllPools();

        var sourceConnectionString =
            new SqliteConnectionStringBuilder
            {
                DataSource =
                    snapshotDatabasePath,

                Mode =
                    SqliteOpenMode.ReadOnly,

                Pooling =
                    false
            };

        await using var sourceConnection =
            new SqliteConnection(
                sourceConnectionString
                    .ToString());

        await sourceConnection.OpenAsync(
            cancellationToken);

        await using HrManagementDbContext dbContext =
            await _dbContextFactory
                .CreateDbContextAsync(
                    cancellationToken);

        if (dbContext.Database
                .GetDbConnection()
            is not SqliteConnection destinationConnection)
        {
            throw new InvalidOperationException(
                "Database hiện tại không sử dụng SQLite.");
        }

        bool destinationWasClosed =
            destinationConnection.State ==
                ConnectionState.Closed;

        if (destinationWasClosed)
        {
            await destinationConnection
                .OpenAsync(
                    cancellationToken);
        }

        try
        {
            cancellationToken
                .ThrowIfCancellationRequested();

            sourceConnection.BackupDatabase(
                destinationConnection);
        }
        finally
        {
            if (destinationWasClosed)
            {
                await destinationConnection
                    .CloseAsync();
            }
        }
    }

    private async Task VerifyRestoredDatabaseAsync(
        DatabaseBackupInfo backupInfo,
        CancellationToken cancellationToken)
    {
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

        bool connectionWasClosed =
            connection.State ==
                ConnectionState.Closed;

        if (connectionWasClosed)
        {
            await connection.OpenAsync(
                cancellationToken);
        }

        try
        {
            await using SqliteCommand quickCheckCommand =
                connection.CreateCommand();

            quickCheckCommand.CommandText =
                "PRAGMA quick_check;";

            object? quickCheckResult =
                await quickCheckCommand
                    .ExecuteScalarAsync(
                        cancellationToken);

            if (!string.Equals(
                    Convert.ToString(
                        quickCheckResult),
                    "ok",
                    StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidDataException(
                    "Database rollback không vượt qua kiểm tra tính toàn vẹn.");
            }

            await using SqliteCommand migrationCommand =
                connection.CreateCommand();

            migrationCommand.CommandText =
                """
                SELECT MigrationId
                FROM __EFMigrationsHistory
                ORDER BY MigrationId DESC
                LIMIT 1;
                """;

            object? latestMigrationResult =
                await migrationCommand
                    .ExecuteScalarAsync(
                        cancellationToken);

            string? latestMigration =
                latestMigrationResult is null
                    || latestMigrationResult is DBNull
                    ? null
                    : Convert.ToString(
                        latestMigrationResult);

            if (!string.Equals(
                    latestMigration,
                    backupInfo.LatestAppliedMigration,
                    StringComparison.Ordinal))
            {
                throw new InvalidDataException(
                    "Database rollback không khớp trạng thái migration của backup.");
            }
        }
        finally
        {
            if (connectionWasClosed)
            {
                await connection.CloseAsync();
            }
        }
    }

    private static void TryDeleteDirectory(
        string directoryPath)
    {
        try
        {
            if (Directory.Exists(
                    directoryPath))
            {
                Directory.Delete(
                    directoryPath,
                    recursive:
                        true);
            }
        }
        catch
        {
            // Cleanup must not mask the upgrade result.
        }
    }
}
