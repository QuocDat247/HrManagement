using HrManagement.Domain.Employees;
using HrManagement.Infrastructure.Persistence;
using HrManagement.Infrastructure.Persistence.Backups;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text.Json;

namespace HrManagement.Tests.Infrastructure.Persistence.Backups;

public sealed class SqliteDatabaseBackupServiceTests
{
    [Fact]
    public async Task
        CreateAsync_CreatesConsistentPackageWithDatabaseAndMetadata()
    {
        string sourceDatabasePath =
            CreateTemporaryPath(
                ".db");

        string backupPath =
            CreateTemporaryPath(
                ".hrbackup");

        string extractedDatabasePath =
            CreateTemporaryPath(
                ".db");

        try
        {
            var factory =
                new TestDbContextFactory(
                    sourceDatabasePath);

            await MigrateAndSeedAsync(
                factory);

            var service =
                new SqliteDatabaseBackupService(
                    factory,
                    new DatabaseInitializationOptions(
                        ApplicationDataMode.Production),
                    TimeProvider.System);

            var result =
                await service.CreateAsync(
                    backupPath);

            Assert.True(
                File.Exists(
                    backupPath));

            Assert.Equal(
                Path.GetFullPath(
                    backupPath),
                result.BackupFilePath);

            using ZipArchive archive =
                ZipFile.OpenRead(
                    backupPath);

            ZipArchiveEntry? databaseEntry =
                archive.GetEntry(
                    "database.sqlite");

            ZipArchiveEntry? metadataEntry =
                archive.GetEntry(
                    "metadata.json");

            Assert.NotNull(
                databaseEntry);

            Assert.NotNull(
                metadataEntry);

            await ExtractEntryAsync(
                databaseEntry!,
                extractedDatabasePath);

            await AssertBackupContainsEmployeeAsync(
                extractedDatabasePath);

            using Stream metadataStream =
                metadataEntry!.Open();

            using JsonDocument metadata =
                await JsonDocument.ParseAsync(
                    metadataStream);

            JsonElement root =
                metadata.RootElement;

            Assert.Equal(
                1,
                root.GetProperty(
                        "FormatVersion")
                    .GetInt32());

            Assert.Equal(
                "Production",
                root.GetProperty(
                        "DataMode")
                    .GetString());

            Assert.Equal(
                "database.sqlite",
                root.GetProperty(
                        "DatabaseEntryName")
                    .GetString());

            string expectedHash =
                await ComputeSha256Async(
                    extractedDatabasePath);

            Assert.Equal(
                expectedHash,
                root.GetProperty(
                        "DatabaseSha256")
                    .GetString());
        }
        finally
        {
            DeleteIfExists(
                sourceDatabasePath);

            DeleteIfExists(
                $"{sourceDatabasePath}-shm");

            DeleteIfExists(
                $"{sourceDatabasePath}-wal");

            DeleteIfExists(
                backupPath);

            DeleteIfExists(
                extractedDatabasePath);
        }
    }

    private static async Task MigrateAndSeedAsync(
        IDbContextFactory<HrManagementDbContext> factory)
    {
        await using HrManagementDbContext dbContext =
            await factory.CreateDbContextAsync();

        await dbContext.Database
            .MigrateAsync();

        dbContext.Employees.Add(
            new Employee(
                Guid.NewGuid(),
                "EMP-BACKUP",
                "Nhân viên Backup",
                "backup@example.com",
                "0901000999",
                new DateOnly(
                    1995,
                    1,
                    1),
                new DateOnly(
                    2024,
                    1,
                    1),
                "Kiểm thử",
                "Nhân viên",
                EmployeeStatus.Active));

        await dbContext.SaveChangesAsync();
    }

    private static async Task
        AssertBackupContainsEmployeeAsync(
            string databasePath)
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

        await connection.OpenAsync();

        await using SqliteCommand command =
            connection.CreateCommand();

        command.CommandText =
            """
            SELECT COUNT(*)
            FROM Employees
            WHERE EmployeeCode = 'EMP-BACKUP';
            """;

        long count =
            Convert.ToInt64(
                await command.ExecuteScalarAsync());

        Assert.Equal(
            1,
            count);

        command.CommandText =
            "PRAGMA quick_check;";

        string? integrityResult =
            Convert.ToString(
                await command.ExecuteScalarAsync());

        Assert.Equal(
            "ok",
            integrityResult);
    }

    private static async Task ExtractEntryAsync(
        ZipArchiveEntry entry,
        string destinationPath)
    {
        await using Stream source =
            entry.Open();

        await using var destination =
            new FileStream(
                destinationPath,
                FileMode.Create,
                FileAccess.Write,
                FileShare.None,
                bufferSize:
                    81920,
                FileOptions.Asynchronous);

        await source.CopyToAsync(
            destination);
    }

    private static async Task<string>
        ComputeSha256Async(
            string filePath)
    {
        await using var stream =
            new FileStream(
                filePath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read);

        using SHA256 sha256 =
            SHA256.Create();

        byte[] hash =
            await sha256.ComputeHashAsync(
                stream);

        return Convert.ToHexString(
            hash);
    }

    private static string CreateTemporaryPath(
        string extension)
    {
        return Path.Combine(
            Path.GetTempPath(),
            $"hrmanagement-backup-{Guid.NewGuid():N}{extension}");
    }

    private static void DeleteIfExists(
        string path)
    {
        if (File.Exists(
                path))
        {
            File.Delete(
                path);
        }
    }

    private sealed class TestDbContextFactory
        : IDbContextFactory<HrManagementDbContext>
    {
        private readonly DbContextOptions<
            HrManagementDbContext>
            _options;

        public TestDbContextFactory(
            string databasePath)
        {
            _options =
                new DbContextOptionsBuilder<
                        HrManagementDbContext>()
                    .UseSqlite(
                        $"Data Source={databasePath};"
                        + "Pooling=False;"
                        + "Default Timeout=30")
                    .Options;
        }

        public HrManagementDbContext CreateDbContext()
        {
            return new HrManagementDbContext(
                _options);
        }

        public Task<HrManagementDbContext>
            CreateDbContextAsync(
                CancellationToken cancellationToken = default)
        {
            cancellationToken
                .ThrowIfCancellationRequested();

            return Task.FromResult(
                CreateDbContext());
        }
    }
}
