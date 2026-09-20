using HrManagement.Application.Persistence.Backups;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text.Json;

namespace HrManagement.Infrastructure.Persistence.Backups;

public sealed class SqliteDatabaseBackupValidationService
    : IDatabaseBackupValidationService
{
    private const int SupportedFormatVersion =
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

    public SqliteDatabaseBackupValidationService(
        IDbContextFactory<HrManagementDbContext> dbContextFactory,
        DatabaseInitializationOptions options)
    {
        _dbContextFactory =
            dbContextFactory;

        _options =
            options;
    }

    public async Task<DatabaseBackupValidationResult>
        ValidateAsync(
            string backupFilePath,
            CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(
                backupFilePath))
        {
            return Invalid(
                "Đường dẫn file backup không hợp lệ.");
        }

        string fullBackupPath;

        try
        {
            fullBackupPath =
                Path.GetFullPath(
                    backupFilePath);
        }
        catch (
            Exception exception)
            when (exception
                is ArgumentException
                or NotSupportedException
                or PathTooLongException)
        {
            return Invalid(
                "Đường dẫn file backup không hợp lệ.");
        }

        if (!File.Exists(
                fullBackupPath))
        {
            return Invalid(
                "Không tìm thấy file backup.");
        }

        string temporaryDirectory =
            Path.Combine(
                Path.GetTempPath(),
                "HrManagement",
                "BackupValidation",
                Guid.NewGuid()
                    .ToString(
                        "N"));

        Directory.CreateDirectory(
            temporaryDirectory);

        string extractedDatabasePath =
            Path.Combine(
                temporaryDirectory,
                DatabaseEntryName);

        try
        {
            cancellationToken
                .ThrowIfCancellationRequested();

            DatabaseBackupPackageMetadata metadata;

            using (
                ZipArchive archive =
                    ZipFile.OpenRead(
                        fullBackupPath))
            {
                if (archive.Entries.Count !=
                    2)
                {
                    return Invalid(
                        "Package backup không đúng cấu trúc.");
                }

                ZipArchiveEntry[] databaseEntries =
                    archive.Entries
                        .Where(
                            entry =>
                                string.Equals(
                                    entry.FullName,
                                    DatabaseEntryName,
                                    StringComparison.Ordinal))
                        .ToArray();

                ZipArchiveEntry[] metadataEntries =
                    archive.Entries
                        .Where(
                            entry =>
                                string.Equals(
                                    entry.FullName,
                                    MetadataEntryName,
                                    StringComparison.Ordinal))
                        .ToArray();

                if (databaseEntries.Length !=
                        1
                    || metadataEntries.Length !=
                        1)
                {
                    return Invalid(
                        "Package backup không đúng cấu trúc.");
                }

                ZipArchiveEntry databaseEntry =
                    databaseEntries[0];

                ZipArchiveEntry metadataEntry =
                    metadataEntries[0];

                if (databaseEntry.Length <=
                    0)
                {
                    return Invalid(
                        "Database trong package backup bị rỗng.");
                }

                await using (
                    Stream metadataStream =
                        metadataEntry.Open())
                {
                    metadata =
                        await JsonSerializer
                            .DeserializeAsync<
                                DatabaseBackupPackageMetadata>(
                                metadataStream,
                                cancellationToken:
                                    cancellationToken)
                        ?? throw new InvalidDataException(
                            "Metadata backup không hợp lệ.");
                }

                DatabaseBackupValidationResult?
                    metadataValidation =
                        ValidateMetadata(
                            metadata);

                if (metadataValidation is not null)
                {
                    return metadataValidation;
                }

                await using Stream source =
                    databaseEntry.Open();

                await using var destination =
                    new FileStream(
                        extractedDatabasePath,
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

            string actualSha256 =
                await ComputeSha256Async(
                    extractedDatabasePath,
                    cancellationToken);

            if (!string.Equals(
                    actualSha256,
                    metadata.DatabaseSha256,
                    StringComparison.OrdinalIgnoreCase))
            {
                return Invalid(
                    "Database trong package backup không khớp checksum.");
            }

            DatabaseBackupValidationResult?
                databaseValidation =
                    await ValidateDatabaseAsync(
                        extractedDatabasePath,
                        metadata,
                        cancellationToken);

            if (databaseValidation is not null)
            {
                return databaseValidation;
            }

            return new DatabaseBackupValidationResult(
                true,
                BackupInfo:
                    new DatabaseBackupInfo(
                        metadata.CreatedUtc,
                        metadata.DataMode,
                        metadata.LatestAppliedMigration));
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (
            Exception exception)
            when (exception
                is InvalidDataException
                or IOException
                or UnauthorizedAccessException
                or JsonException
                or SqliteException)
        {
            return Invalid(
                "File backup bị hỏng hoặc không phải package backup hợp lệ.");
        }
        finally
        {
            TryDeleteDirectory(
                temporaryDirectory);
        }
    }

    private DatabaseBackupValidationResult?
        ValidateMetadata(
            DatabaseBackupPackageMetadata metadata)
    {
        if (metadata.FormatVersion !=
            SupportedFormatVersion)
        {
            return Invalid(
                "Phiên bản package backup không được hỗ trợ.");
        }

        if (!string.Equals(
                metadata.DatabaseEntryName,
                DatabaseEntryName,
                StringComparison.Ordinal))
        {
            return Invalid(
                "Metadata của package backup không hợp lệ.");
        }

        if (string.IsNullOrWhiteSpace(
                metadata.DatabaseSha256))
        {
            return Invalid(
                "Package backup không có checksum database.");
        }

        if (!string.Equals(
                metadata.DataMode,
                _options.DataMode.ToString(),
                StringComparison.Ordinal))
        {
            return Invalid(
                "Backup này thuộc chế độ dữ liệu khác với database hiện tại.");
        }

        return null;
    }

    private async Task<DatabaseBackupValidationResult?>
        ValidateDatabaseAsync(
            string databasePath,
            DatabaseBackupPackageMetadata metadata,
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

        await using (
            SqliteCommand quickCheckCommand =
                connection.CreateCommand())
        {
            quickCheckCommand.CommandText =
                "PRAGMA quick_check;";

            object? result =
                await quickCheckCommand
                    .ExecuteScalarAsync(
                        cancellationToken);

            if (!string.Equals(
                    Convert.ToString(
                        result),
                    "ok",
                    StringComparison.OrdinalIgnoreCase))
            {
                return Invalid(
                    "Database trong backup không vượt qua kiểm tra tính toàn vẹn.");
            }
        }

        List<string> appliedMigrations =
            new();

        await using (
            SqliteCommand migrationCommand =
                connection.CreateCommand())
        {
            migrationCommand.CommandText =
                """
                SELECT MigrationId
                FROM __EFMigrationsHistory
                ORDER BY MigrationId;
                """;

            await using SqliteDataReader reader =
                await migrationCommand
                    .ExecuteReaderAsync(
                        cancellationToken);

            while (await reader.ReadAsync(
                       cancellationToken))
            {
                appliedMigrations.Add(
                    reader.GetString(
                        0));
            }
        }

        string? actualLatestMigration =
            appliedMigrations
                .LastOrDefault();

        if (!string.Equals(
                actualLatestMigration,
                metadata.LatestAppliedMigration,
                StringComparison.Ordinal))
        {
            return Invalid(
                "Lịch sử migration trong database không khớp metadata backup.");
        }

        await using HrManagementDbContext dbContext =
            await _dbContextFactory
                .CreateDbContextAsync(
                    cancellationToken);

        cancellationToken
            .ThrowIfCancellationRequested();

        string[] knownMigrations =
            dbContext.Database
                .GetMigrations()
                .ToArray();

        if (appliedMigrations.Count >
            knownMigrations.Length)
        {
            return Invalid(
                "Backup được tạo bởi phiên bản ứng dụng mới hơn.");
        }

        bool migrationHistoryMatches =
            appliedMigrations.SequenceEqual(
                knownMigrations.Take(
                    appliedMigrations.Count),
                StringComparer.Ordinal);

        if (!migrationHistoryMatches)
        {
            return Invalid(
                "Lịch sử migration của backup không tương thích với ứng dụng hiện tại.");
        }

        return null;
    }

    private static async Task<string>
        ComputeSha256Async(
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

    private static DatabaseBackupValidationResult
        Invalid(
            string message)
    {
        return new DatabaseBackupValidationResult(
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
            // Validation cleanup must not mask
            // the original validation result.
        }
    }
}
