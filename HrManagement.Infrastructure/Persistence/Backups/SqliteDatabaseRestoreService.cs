using HrManagement.Application.Persistence.Backups;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using System.Data;
using System.IO.Compression;

namespace HrManagement.Infrastructure.Persistence.Backups;

public sealed class SqliteDatabaseRestoreService
    : IDatabaseRestoreService
{
    private readonly IDatabaseRestoreFinalizer
        _restoreFinalizer;

    private const string DatabaseEntryName =
        "database.sqlite";

    private readonly IDbContextFactory<
        HrManagementDbContext>
        _dbContextFactory;

    private readonly IDatabaseBackupService
        _backupService;

    private readonly IDatabaseBackupValidationService
        _validationService;

    private readonly TimeProvider
        _timeProvider;

    private readonly SemaphoreSlim
        _restoreGate =
            new(
                1,
                1);

    public SqliteDatabaseRestoreService(
        IDbContextFactory<HrManagementDbContext> dbContextFactory,
        IDatabaseBackupService backupService,
        IDatabaseBackupValidationService validationService,
        IDatabaseRestoreFinalizer restoreFinalizer,
        TimeProvider timeProvider)
    {
        _restoreFinalizer =
            restoreFinalizer;

        _dbContextFactory =
            dbContextFactory;

        _backupService =
            backupService;

        _validationService =
            validationService;

        _timeProvider =
            timeProvider;
    }

    public async Task<DatabaseRestoreResult> RestoreAsync(
        string backupFilePath,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(
                backupFilePath))
        {
            return Failure(
                "Đường dẫn file backup không hợp lệ.");
        }

        await _restoreGate.WaitAsync(
            cancellationToken);

        string temporaryDirectory =
            Path.Combine(
                Path.GetTempPath(),
                "HrManagement",
                "Restore",
                Guid.NewGuid()
                    .ToString(
                        "N"));

        Directory.CreateDirectory(
            temporaryDirectory);

        string stagedPackagePath =
            Path.Combine(
                temporaryDirectory,
                "restore.hrbackup");

        string extractedDatabasePath =
            Path.Combine(
                temporaryDirectory,
                DatabaseEntryName);

        string? safetyBackupFilePath =
            null;

        DatabaseBackupInfo? backupInfo =
            null;

        bool destinationTouched =
            false;

        try
        {
            await StagePackageAsync(
                backupFilePath,
                stagedPackagePath,
                cancellationToken);

            DatabaseBackupValidationResult validation =
                await _validationService
                    .ValidateAsync(
                        stagedPackagePath,
                        cancellationToken);

            if (!validation.IsValid)
            {
                return Failure(
                    validation.ErrorMessage
                        ?? "File backup không hợp lệ.");
            }

            backupInfo =
                validation.BackupInfo;

            string liveDatabasePath =
                await GetLiveDatabasePathAsync(
                    cancellationToken);

            safetyBackupFilePath =
                CreateSafetyBackupPath(
                    liveDatabasePath);

            await _backupService
                .CreateAsync(
                    safetyBackupFilePath,
                    cancellationToken);

            await ExtractDatabaseAsync(
                stagedPackagePath,
                extractedDatabasePath,
                cancellationToken);

            destinationTouched =
                true;

            await RestoreSnapshotAsync(
                extractedDatabasePath,
                cancellationToken);

            SqliteConnection.ClearAllPools();

            await _restoreFinalizer
                .FinalizeAsync(
                    cancellationToken);

            return new DatabaseRestoreResult(
                true,
                SafetyBackupFilePath:
                    safetyBackupFilePath,
                BackupInfo:
                    backupInfo);
        }
        catch (OperationCanceledException)
        {
            if (destinationTouched
                && !string.IsNullOrWhiteSpace(
                    safetyBackupFilePath))
            {
                await RollBackAsync(
                    safetyBackupFilePath);
            }

            throw;
        }
        catch (Exception restoreException)
        {
            if (!destinationTouched
                || string.IsNullOrWhiteSpace(
                    safetyBackupFilePath))
            {
                return new DatabaseRestoreResult(
                    false,
                    "Không thể chuẩn bị hoặc thực hiện khôi phục database.",
                    SafetyBackupFilePath:
                        safetyBackupFilePath,
                    BackupInfo:
                        backupInfo);
            }

            try
            {
                await RollBackAsync(
                    safetyBackupFilePath);

                return new DatabaseRestoreResult(
                    false,
                    "Khôi phục database thất bại. "
                    + "Database trước đó đã được phục hồi an toàn.",
                    SafetyBackupFilePath:
                        safetyBackupFilePath,
                    WasRolledBack:
                        true,
                    BackupInfo:
                        backupInfo);
            }
            catch (Exception rollbackException)
            {
                throw new InvalidOperationException(
                    "Khôi phục database thất bại và rollback tự động cũng thất bại. "
                    + $"Safety backup được lưu tại: {safetyBackupFilePath}",
                    new AggregateException(
                        restoreException,
                        rollbackException));
            }
        }
        finally
        {
            TryDeleteDirectory(
                temporaryDirectory);

            _restoreGate.Release();
        }
    }

    private async Task<string>
        GetLiveDatabasePathAsync(
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

        return Path.GetFullPath(
            connection.DataSource);
    }

    private string CreateSafetyBackupPath(
        string liveDatabasePath)
    {
        string? databaseDirectory =
            Path.GetDirectoryName(
                liveDatabasePath);

        if (string.IsNullOrWhiteSpace(
                databaseDirectory))
        {
            throw new InvalidOperationException(
                "Không thể xác định thư mục database.");
        }

        string safetyDirectory =
            Path.Combine(
                databaseDirectory,
                "Backups",
                "Safety");

        Directory.CreateDirectory(
            safetyDirectory);

        DateTimeOffset nowUtc =
            _timeProvider.GetUtcNow();

        string timestamp =
            nowUtc.ToString(
                "yyyyMMdd-HHmmss");

        string uniqueSuffix =
            Guid.NewGuid()
                .ToString(
                    "N")[..8];

        return Path.Combine(
            safetyDirectory,
            $"PreRestore-{timestamp}-{uniqueSuffix}.hrbackup");
    }

    private static async Task StagePackageAsync(
        string sourcePath,
        string destinationPath,
        CancellationToken cancellationToken)
    {
        string fullSourcePath =
            Path.GetFullPath(
                sourcePath);

        if (!File.Exists(
                fullSourcePath))
        {
            throw new FileNotFoundException(
                "Không tìm thấy file backup.",
                fullSourcePath);
        }

        await using var source =
            new FileStream(
                fullSourcePath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read,
                bufferSize:
                    81920,
                FileOptions.Asynchronous
                    | FileOptions.SequentialScan);

        await using var destination =
            new FileStream(
                destinationPath,
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
                "Package backup không chứa database.");
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

    private async Task MigrateAndVerifyAsync(
        CancellationToken cancellationToken)
    {
        await using HrManagementDbContext dbContext =
            await _dbContextFactory
                .CreateDbContextAsync(
                    cancellationToken);

        await dbContext.Database
            .MigrateAsync(
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
            await using SqliteCommand command =
                connection.CreateCommand();

            command.CommandText =
                "PRAGMA quick_check;";

            object? result =
                await command.ExecuteScalarAsync(
                    cancellationToken);

            if (!string.Equals(
                    Convert.ToString(
                        result),
                    "ok",
                    StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidDataException(
                    "Database sau restore không vượt qua kiểm tra tính toàn vẹn.");
            }

            string[] expectedMigrations =
                dbContext.Database
                    .GetMigrations()
                    .ToArray();

            string[] appliedMigrations =
                (await dbContext.Database
                    .GetAppliedMigrationsAsync(
                        cancellationToken))
                .ToArray();

            if (!appliedMigrations.SequenceEqual(
                    expectedMigrations,
                    StringComparer.Ordinal))
            {
                throw new InvalidDataException(
                    "Database sau restore chưa ở đúng migration hiện tại.");
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

    private async Task RollBackAsync(
        string safetyBackupFilePath)
    {
        DatabaseBackupValidationResult validation =
            await _validationService
                .ValidateAsync(
                    safetyBackupFilePath,
                    CancellationToken.None);

        if (!validation.IsValid)
        {
            throw new InvalidDataException(
                "Safety backup không hợp lệ.");
        }

        string rollbackDirectory =
            Path.Combine(
                Path.GetTempPath(),
                "HrManagement",
                "Rollback",
                Guid.NewGuid()
                    .ToString(
                        "N"));

        Directory.CreateDirectory(
            rollbackDirectory);

        string rollbackDatabasePath =
            Path.Combine(
                rollbackDirectory,
                DatabaseEntryName);

        try
        {
            await ExtractDatabaseAsync(
                safetyBackupFilePath,
                rollbackDatabasePath,
                CancellationToken.None);

            await RestoreSnapshotAsync(
                rollbackDatabasePath,
                CancellationToken.None);

            SqliteConnection.ClearAllPools();

            await VerifyCurrentDatabaseAsync(
                CancellationToken.None);
        }
        finally
        {
            TryDeleteDirectory(
                rollbackDirectory);
        }
    }

    private async Task VerifyCurrentDatabaseAsync(
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
            await using SqliteCommand command =
                connection.CreateCommand();

            command.CommandText =
                "PRAGMA quick_check;";

            object? result =
                await command.ExecuteScalarAsync(
                    cancellationToken);

            if (!string.Equals(
                    Convert.ToString(
                        result),
                    "ok",
                    StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidDataException(
                    "Database rollback không vượt qua kiểm tra tính toàn vẹn.");
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

    private static DatabaseRestoreResult Failure(
        string message)
    {
        return new DatabaseRestoreResult(
            false,
            message);
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
            // Restore cleanup must not mask
            // the actual restore result.
        }
    }
}
