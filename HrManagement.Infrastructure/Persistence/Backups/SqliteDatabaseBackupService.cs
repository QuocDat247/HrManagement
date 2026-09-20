using HrManagement.Application.Persistence.Backups;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using System.Data;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text.Json;

namespace HrManagement.Infrastructure.Persistence.Backups;

public sealed class SqliteDatabaseBackupService
    : IDatabaseBackupService
{
    private const int BackupFormatVersion =
        1;

    private const string DatabaseEntryName =
        "database.sqlite";

    private const string MetadataEntryName =
        "metadata.json";

    private readonly IDbContextFactory<
        HrManagementDbContext>
        _dbContextFactory;

    private readonly DatabaseInitializationOptions
        _options;

    private readonly TimeProvider
        _timeProvider;

    public SqliteDatabaseBackupService(
        IDbContextFactory<HrManagementDbContext> dbContextFactory,
        DatabaseInitializationOptions options,
        TimeProvider timeProvider)
    {
        _dbContextFactory =
            dbContextFactory;

        _options =
            options;

        _timeProvider =
            timeProvider;
    }

    public async Task<DatabaseBackupResult> CreateAsync(
        string destinationFilePath,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(
                destinationFilePath))
        {
            throw new ArgumentException(
                "Đường dẫn backup không được để trống.",
                nameof(destinationFilePath));
        }

        cancellationToken
            .ThrowIfCancellationRequested();

        string fullDestinationPath =
            Path.GetFullPath(
                destinationFilePath);

        string? destinationDirectory =
            Path.GetDirectoryName(
                fullDestinationPath);

        if (string.IsNullOrWhiteSpace(
                destinationDirectory))
        {
            throw new ArgumentException(
                "Thư mục lưu backup không hợp lệ.",
                nameof(destinationFilePath));
        }

        Directory.CreateDirectory(
            destinationDirectory);

        string temporaryDirectory =
            Path.Combine(
                Path.GetTempPath(),
                "HrManagement",
                "Backups",
                Guid.NewGuid()
                    .ToString(
                        "N"));

        Directory.CreateDirectory(
            temporaryDirectory);

        string temporaryDatabasePath =
            Path.Combine(
                temporaryDirectory,
                DatabaseEntryName);

        string temporaryPackagePath =
            Path.Combine(
                destinationDirectory,
                $".{Path.GetFileName(fullDestinationPath)}."
                + $"{Guid.NewGuid():N}.tmp");

        try
        {
            DateTimeOffset createdUtc =
                _timeProvider
                    .GetUtcNow();

            string? latestAppliedMigration =
                await CreateConsistentDatabaseSnapshotAsync(
                    temporaryDatabasePath,
                    cancellationToken);

            await VerifySnapshotAsync(
                temporaryDatabasePath,
                cancellationToken);

            string databaseSha256 =
                await ComputeSha256Async(
                    temporaryDatabasePath,
                    cancellationToken);

            var metadata =
                new DatabaseBackupPackageMetadata(
                    BackupFormatVersion,
                    createdUtc,
                    _options.DataMode.ToString(),
                    latestAppliedMigration,
                    DatabaseEntryName,
                    databaseSha256);

            await CreatePackageAsync(
                temporaryPackagePath,
                temporaryDatabasePath,
                metadata,
                cancellationToken);

            File.Move(
                temporaryPackagePath,
                fullDestinationPath,
                overwrite:
                    true);

            return new DatabaseBackupResult(
                fullDestinationPath,
                createdUtc,
                latestAppliedMigration);
        }
        finally
        {
            TryDeleteFile(
                temporaryPackagePath);

            TryDeleteDirectory(
                temporaryDirectory);
        }
    }

    private async Task<string?>
        CreateConsistentDatabaseSnapshotAsync(
            string destinationDatabasePath,
            CancellationToken cancellationToken)
    {
        await using HrManagementDbContext dbContext =
            await _dbContextFactory
                .CreateDbContextAsync(
                    cancellationToken);

        IReadOnlyList<string> appliedMigrations =
            (await dbContext.Database
                .GetAppliedMigrationsAsync(
                    cancellationToken))
            .ToArray();

        string? latestAppliedMigration =
            appliedMigrations
                .LastOrDefault();

        if (dbContext.Database
                .GetDbConnection()
            is not SqliteConnection sourceConnection)
        {
            throw new InvalidOperationException(
                "Database hiện tại không sử dụng SQLite.");
        }

        bool sourceWasClosed =
            sourceConnection.State ==
                ConnectionState.Closed;

        if (sourceWasClosed)
        {
            await sourceConnection
                .OpenAsync(
                    cancellationToken);
        }

        try
        {
            var destinationConnectionString =
                new SqliteConnectionStringBuilder
                {
                    DataSource =
                        destinationDatabasePath,

                    Mode =
                        SqliteOpenMode.ReadWriteCreate,

                    Pooling =
                        false
                };

            await using var destinationConnection =
                new SqliteConnection(
                    destinationConnectionString
                        .ToString());

            await destinationConnection
                .OpenAsync(
                    cancellationToken);

            cancellationToken
                .ThrowIfCancellationRequested();

            sourceConnection.BackupDatabase(
                destinationConnection);

            return latestAppliedMigration;
        }
        finally
        {
            if (sourceWasClosed)
            {
                await sourceConnection
                    .CloseAsync();
            }
        }
    }

    private static async Task VerifySnapshotAsync(
        string databasePath,
        CancellationToken cancellationToken)
    {
        var connectionString =
            new SqliteConnectionStringBuilder
            {
                DataSource =
                    databasePath,

                Mode =
                    SqliteOpenMode.ReadOnly,

                Pooling =
                    false
            };

        await using var connection =
            new SqliteConnection(
                connectionString.ToString());

        await connection.OpenAsync(
            cancellationToken);

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
                "SQLite snapshot không vượt qua kiểm tra tính toàn vẹn.");
        }
    }

    private static async Task<string> ComputeSha256Async(
        string filePath,
        CancellationToken cancellationToken)
    {
        await using var stream =
            new FileStream(
                filePath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read,
                bufferSize:
                    81920,
                FileOptions.Asynchronous
                    | FileOptions.SequentialScan);

        using SHA256 sha256 =
            SHA256.Create();

        byte[] hash =
            await sha256.ComputeHashAsync(
                stream,
                cancellationToken);

        return Convert.ToHexString(
            hash);
    }

    private static async Task CreatePackageAsync(
        string packagePath,
        string databasePath,
        DatabaseBackupPackageMetadata metadata,
        CancellationToken cancellationToken)
    {
        await using var packageStream =
            new FileStream(
                packagePath,
                FileMode.CreateNew,
                FileAccess.ReadWrite,
                FileShare.None,
                bufferSize:
                    81920,
                FileOptions.Asynchronous);

        using var archive =
            new ZipArchive(
                packageStream,
                ZipArchiveMode.Create,
                leaveOpen:
                    true);

        ZipArchiveEntry databaseEntry =
            archive.CreateEntry(
                DatabaseEntryName,
                CompressionLevel.Optimal);

        await using (
            Stream databaseEntryStream =
                databaseEntry.Open())
        await using (
            var databaseStream =
                new FileStream(
                    databasePath,
                    FileMode.Open,
                    FileAccess.Read,
                    FileShare.Read,
                    bufferSize:
                        81920,
                    FileOptions.Asynchronous
                        | FileOptions.SequentialScan))
        {
            await databaseStream.CopyToAsync(
                databaseEntryStream,
                cancellationToken);
        }

        ZipArchiveEntry metadataEntry =
            archive.CreateEntry(
                MetadataEntryName,
                CompressionLevel.Optimal);

        await using Stream metadataStream =
            metadataEntry.Open();

        await JsonSerializer.SerializeAsync(
            metadataStream,
            metadata,
            cancellationToken:
                cancellationToken);
    }

    private static void TryDeleteFile(
        string filePath)
    {
        try
        {
            if (File.Exists(
                    filePath))
            {
                File.Delete(
                    filePath);
            }
        }
        catch
        {
            // Temporary cleanup must not mask
            // the original backup result.
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
            // Temporary cleanup must not mask
            // the original backup result.
        }
    }
}
