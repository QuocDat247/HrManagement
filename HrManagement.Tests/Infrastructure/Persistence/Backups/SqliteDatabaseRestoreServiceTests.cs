using HrManagement.Application.Persistence.Backups;
using HrManagement.Domain.Employees;
using HrManagement.Infrastructure.Persistence;
using HrManagement.Infrastructure.Persistence.Backups;
using Microsoft.EntityFrameworkCore;
using System.IO.Compression;

namespace HrManagement.Tests.Infrastructure.Persistence.Backups;

public sealed class SqliteDatabaseRestoreServiceTests
{
    [Fact]
    public async Task
        RestoreAsync_WithValidBackup_ReplacesDatabaseAndCreatesSafetyBackup()
    {
        string rootDirectory =
            CreateTemporaryDirectory();

        try
        {
            string liveDatabasePath =
                Path.Combine(
                    rootDirectory,
                    "live.db");

            string sourceDatabasePath =
                Path.Combine(
                    rootDirectory,
                    "source.db");

            string backupPath =
                Path.Combine(
                    rootDirectory,
                    "source.hrbackup");

            var liveFactory =
                new TestDbContextFactory(
                    liveDatabasePath);

            var sourceFactory =
                new TestDbContextFactory(
                    sourceDatabasePath);

            await MigrateAndSeedAsync(
                liveFactory,
                "EMP-LIVE");

            await MigrateAndSeedAsync(
                sourceFactory,
                "EMP-BACKUP");

            var options =
                new DatabaseInitializationOptions(
                    ApplicationDataMode.Production);

            var sourceBackupService =
                new SqliteDatabaseBackupService(
                    sourceFactory,
                    options,
                    TimeProvider.System);

            await sourceBackupService
                .CreateAsync(
                    backupPath);

            var liveBackupService =
                new SqliteDatabaseBackupService(
                    liveFactory,
                    options,
                    TimeProvider.System);

            var validationService =
                new SqliteDatabaseBackupValidationService(
                    liveFactory,
                    options);

            var restoreFinalizer =
                new SqliteDatabaseRestoreFinalizer(
                    liveFactory);

            var restoreService =
                new SqliteDatabaseRestoreService(
                    liveFactory,
                    liveBackupService,
                    validationService,
                    restoreFinalizer,
                    TimeProvider.System);

            DatabaseRestoreResult result =
                await restoreService
                    .RestoreAsync(
                        backupPath);

            Assert.True(
                result.IsSuccessful);

            Assert.False(
                result.WasRolledBack);

            Assert.False(
                string.IsNullOrWhiteSpace(
                    result.SafetyBackupFilePath));

            Assert.True(
                File.Exists(
                    result.SafetyBackupFilePath));

            await AssertEmployeeExistsAsync(
                liveFactory,
                "EMP-BACKUP",
                expected:
                    true);

            await AssertEmployeeExistsAsync(
                liveFactory,
                "EMP-LIVE",
                expected:
                    false);

            await AssertSafetyBackupContainsEmployeeAsync(
                result.SafetyBackupFilePath!,
                "EMP-LIVE",
                rootDirectory);
        }
        finally
        {
            DeleteDirectory(
                rootDirectory);
        }
    }

    [Fact]
    public async Task
        RestoreAsync_WithInvalidBackup_DoesNotChangeLiveDatabase()
    {
        string rootDirectory =
            CreateTemporaryDirectory();

        try
        {
            string liveDatabasePath =
                Path.Combine(
                    rootDirectory,
                    "live.db");

            string invalidBackupPath =
                Path.Combine(
                    rootDirectory,
                    "invalid.hrbackup");

            var liveFactory =
                new TestDbContextFactory(
                    liveDatabasePath);

            await MigrateAndSeedAsync(
                liveFactory,
                "EMP-LIVE");

            await File.WriteAllTextAsync(
                invalidBackupPath,
                "not-a-backup");

            var options =
                new DatabaseInitializationOptions(
                    ApplicationDataMode.Production);

            var backupService =
                new SqliteDatabaseBackupService(
                    liveFactory,
                    options,
                    TimeProvider.System);

            var validationService =
                new SqliteDatabaseBackupValidationService(
                    liveFactory,
                    options);

            var restoreFinalizer =
                new SqliteDatabaseRestoreFinalizer(
                    liveFactory);

            var restoreService =
                new SqliteDatabaseRestoreService(
                    liveFactory,
                    backupService,
                    validationService,
                    restoreFinalizer,
                    TimeProvider.System);

            DatabaseRestoreResult result =
                await restoreService
                    .RestoreAsync(
                        invalidBackupPath);

            Assert.False(
                result.IsSuccessful);

            Assert.Null(
                result.SafetyBackupFilePath);

            await AssertEmployeeExistsAsync(
                liveFactory,
                "EMP-LIVE",
                expected:
                    true);
        }
        finally
        {
            DeleteDirectory(
                rootDirectory);
        }
    }

    [Fact]
    public async Task
    RestoreAsync_WhenFinalizationFails_RollsBackOriginalDatabase()
    {
        string rootDirectory =
            CreateTemporaryDirectory();

        try
        {
            string liveDatabasePath =
                Path.Combine(
                    rootDirectory,
                    "live.db");

            string sourceDatabasePath =
                Path.Combine(
                    rootDirectory,
                    "source.db");

            string backupPath =
                Path.Combine(
                    rootDirectory,
                    "source.hrbackup");

            var liveFactory =
                new TestDbContextFactory(
                    liveDatabasePath);

            var sourceFactory =
                new TestDbContextFactory(
                    sourceDatabasePath);

            await MigrateAndSeedAsync(
                liveFactory,
                "EMP-LIVE");

            await MigrateAndSeedAsync(
                sourceFactory,
                "EMP-BACKUP");

            var options =
                new DatabaseInitializationOptions(
                    ApplicationDataMode.Production);

            var sourceBackupService =
                new SqliteDatabaseBackupService(
                    sourceFactory,
                    options,
                    TimeProvider.System);

            await sourceBackupService
                .CreateAsync(
                    backupPath);

            var liveBackupService =
                new SqliteDatabaseBackupService(
                    liveFactory,
                    options,
                    TimeProvider.System);

            var validationService =
                new SqliteDatabaseBackupValidationService(
                    liveFactory,
                    options);

            var restoreService =
                new SqliteDatabaseRestoreService(
                    liveFactory,
                    liveBackupService,
                    validationService,
                    new FailingRestoreFinalizer(),
                    TimeProvider.System);

            DatabaseRestoreResult result =
                await restoreService
                    .RestoreAsync(
                        backupPath);

            Assert.False(
                result.IsSuccessful);

            Assert.True(
                result.WasRolledBack);

            Assert.False(
                string.IsNullOrWhiteSpace(
                    result.SafetyBackupFilePath));

            Assert.True(
                File.Exists(
                    result.SafetyBackupFilePath));

            await AssertEmployeeExistsAsync(
                liveFactory,
                "EMP-LIVE",
                expected:
                    true);

            await AssertEmployeeExistsAsync(
                liveFactory,
                "EMP-BACKUP",
                expected:
                    false);
        }
        finally
        {
            DeleteDirectory(
                rootDirectory);
        }
    }

    private static async Task MigrateAndSeedAsync(
        IDbContextFactory<HrManagementDbContext> factory,
        string employeeCode)
    {
        await using HrManagementDbContext dbContext =
            await factory.CreateDbContextAsync();

        await dbContext.Database
            .MigrateAsync();

        dbContext.Employees.Add(
            new Employee(
                Guid.NewGuid(),
                employeeCode,
                $"Nhân viên {employeeCode}",
                null,
                null,
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
        AssertEmployeeExistsAsync(
            IDbContextFactory<HrManagementDbContext> factory,
            string employeeCode,
            bool expected)
    {
        await using HrManagementDbContext dbContext =
            await factory.CreateDbContextAsync();

        bool exists =
            await dbContext.Employees
                .AsNoTracking()
                .AnyAsync(
                    employee =>
                        employee.EmployeeCode ==
                        employeeCode);

        Assert.Equal(
            expected,
            exists);
    }

    private static async Task
        AssertSafetyBackupContainsEmployeeAsync(
            string backupPath,
            string employeeCode,
            string rootDirectory)
    {
        string extractedDatabasePath =
            Path.Combine(
                rootDirectory,
                $"safety-{Guid.NewGuid():N}.db");

        using (
            ZipArchive archive =
                ZipFile.OpenRead(
                    backupPath))
        {
            ZipArchiveEntry? entry =
                archive.GetEntry(
                    "database.sqlite");

            Assert.NotNull(
                entry);

            await using Stream source =
                entry!.Open();

            await using var destination =
                new FileStream(
                    extractedDatabasePath,
                    FileMode.CreateNew,
                    FileAccess.Write,
                    FileShare.None);

            await source.CopyToAsync(
                destination);
        }

        var factory =
            new TestDbContextFactory(
                extractedDatabasePath);

        await AssertEmployeeExistsAsync(
            factory,
            employeeCode,
            expected:
                true);
    }

    private static string
        CreateTemporaryDirectory()
    {
        string directory =
            Path.Combine(
                Path.GetTempPath(),
                $"hrmanagement-restore-{Guid.NewGuid():N}");

        Directory.CreateDirectory(
            directory);

        return directory;
    }

    private static void DeleteDirectory(
        string directory)
    {
        try
        {
            if (Directory.Exists(
                    directory))
            {
                Directory.Delete(
                    directory,
                    recursive:
                        true);
            }
        }
        catch
        {
            // Test cleanup only.
        }
    }

    private sealed class FailingRestoreFinalizer
    : IDatabaseRestoreFinalizer
    {
        public Task FinalizeAsync(
            CancellationToken cancellationToken = default)
        {
            cancellationToken
                .ThrowIfCancellationRequested();

            throw new InvalidDataException(
                "Forced post-restore failure.");
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
