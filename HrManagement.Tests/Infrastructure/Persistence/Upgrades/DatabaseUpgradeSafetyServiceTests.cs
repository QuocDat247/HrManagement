using HrManagement.Application.Persistence.Backups;
using HrManagement.Infrastructure.Persistence;
using HrManagement.Infrastructure.Persistence.Backups;
using HrManagement.Infrastructure.Persistence.Upgrades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace HrManagement.Tests.Infrastructure.Persistence.Upgrades;

public sealed class
    DatabaseUpgradeSafetyServiceTests
{
    [Fact]
    public async Task
        CreatePreMigrationBackupIfNeededAsync_WithCurrentDatabase_DoesNotCreateBackup()
    {
        string rootDirectory =
            CreateTemporaryDirectory();

        try
        {
            string databasePath =
                Path.Combine(
                    rootDirectory,
                    "current.db");

            var factory =
                new TestDbContextFactory(
                    databasePath);

            await using (
                HrManagementDbContext dbContext =
                    await factory.CreateDbContextAsync())
            {
                await dbContext.Database
                    .MigrateAsync();
            }

            var options =
                new DatabaseInitializationOptions(
                    ApplicationDataMode.Production);

            var backupService =
                new SqliteDatabaseBackupService(
                    factory,
                    options,
                    TimeProvider.System);

            var service =
                new DatabaseUpgradeSafetyService(
                    factory,
                    backupService,
                    TimeProvider.System);

            string? result =
                await service
                    .CreatePreMigrationBackupIfNeededAsync();

            Assert.Null(
                result);

            string upgradeBackupDirectory =
                Path.Combine(
                    rootDirectory,
                    "Backups",
                    "Upgrades");

            Assert.False(
                Directory.Exists(
                    upgradeBackupDirectory));
        }
        finally
        {
            DeleteDirectory(
                rootDirectory);
        }
    }

    [Fact]
    public async Task
        CreatePreMigrationBackupIfNeededAsync_WithPendingMigration_CreatesValidBackup()
    {
        string rootDirectory =
            CreateTemporaryDirectory();

        try
        {
            string databasePath =
                Path.Combine(
                    rootDirectory,
                    "older.db");

            var factory =
                new TestDbContextFactory(
                    databasePath);

            string previousMigration;

            await using (
                HrManagementDbContext dbContext =
                    await factory.CreateDbContextAsync())
            {
                string[] migrations =
                    dbContext.Database
                        .GetMigrations()
                        .ToArray();

                Assert.True(
                    migrations.Length >=
                    2);

                previousMigration =
                    migrations[^2];

                IMigrator migrator =
                    dbContext.GetService<
                        IMigrator>();

                await migrator.MigrateAsync(
                    previousMigration);
            }

            var options =
                new DatabaseInitializationOptions(
                    ApplicationDataMode.Production);

            var backupService =
                new SqliteDatabaseBackupService(
                    factory,
                    options,
                    TimeProvider.System);

            var service =
                new DatabaseUpgradeSafetyService(
                    factory,
                    backupService,
                    TimeProvider.System);

            string? backupPath =
                await service
                    .CreatePreMigrationBackupIfNeededAsync();

            Assert.False(
                string.IsNullOrWhiteSpace(
                    backupPath));

            Assert.True(
                File.Exists(
                    backupPath));

            var validationService =
                new SqliteDatabaseBackupValidationService(
                    factory,
                    options);

            DatabaseBackupValidationResult validation =
                await validationService
                    .ValidateAsync(
                        backupPath!);

            Assert.True(
                validation.IsValid);

            Assert.Equal(
                previousMigration,
                validation.BackupInfo!
                    .LatestAppliedMigration);

            await using HrManagementDbContext verifyContext =
                await factory.CreateDbContextAsync();

            string[] pendingMigrations =
                (await verifyContext.Database
                    .GetPendingMigrationsAsync())
                .ToArray();

            Assert.NotEmpty(
                pendingMigrations);
        }
        finally
        {
            DeleteDirectory(
                rootDirectory);
        }
    }

    [Fact]
    public async Task
        CreatePreMigrationBackupIfNeededAsync_WithFreshDatabase_DoesNotCreateDatabaseOrBackup()
    {
        string rootDirectory =
            CreateTemporaryDirectory();

        try
        {
            string databasePath =
                Path.Combine(
                    rootDirectory,
                    "fresh.db");

            var factory =
                new TestDbContextFactory(
                    databasePath);

            var options =
                new DatabaseInitializationOptions(
                    ApplicationDataMode.Production);

            var backupService =
                new SqliteDatabaseBackupService(
                    factory,
                    options,
                    TimeProvider.System);

            var service =
                new DatabaseUpgradeSafetyService(
                    factory,
                    backupService,
                    TimeProvider.System);

            string? result =
                await service
                    .CreatePreMigrationBackupIfNeededAsync();

            Assert.Null(
                result);

            Assert.False(
                File.Exists(
                    databasePath));
        }
        finally
        {
            DeleteDirectory(
                rootDirectory);
        }
    }

    private static string
        CreateTemporaryDirectory()
    {
        string directory =
            Path.Combine(
                Path.GetTempPath(),
                $"hrmanagement-upgrade-{Guid.NewGuid():N}");

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
