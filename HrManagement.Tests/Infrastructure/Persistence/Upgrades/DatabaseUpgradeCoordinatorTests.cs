using HrManagement.Application.Persistence.Backups;
using HrManagement.Infrastructure.Persistence;
using HrManagement.Infrastructure.Persistence.Backups;
using HrManagement.Infrastructure.Persistence.Upgrades;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace HrManagement.Tests.Infrastructure.Persistence.Upgrades;

public sealed class DatabaseUpgradeCoordinatorTests
{
    [Fact]
    public async Task
        UpgradeAsync_WhenMigrationFailsAfterMutation_RestoresPreUpgradeDatabase()
    {
        string rootDirectory =
            CreateTemporaryDirectory();

        try
        {
            string databasePath =
                Path.Combine(
                    rootDirectory,
                    "upgrade-failure.db");

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

            var validationService =
                new SqliteDatabaseBackupValidationService(
                    factory,
                    options);

            var safetyService =
                new DatabaseUpgradeSafetyService(
                    factory,
                    backupService,
                    TimeProvider.System);

            var rollbackService =
                new DatabaseUpgradeRollbackService(
                    factory,
                    validationService);

            var migrationExecutor =
                new PartiallyFailingMigrationExecutor(
                    databasePath);

            var coordinator =
                new DatabaseUpgradeCoordinator(
                    safetyService,
                    migrationExecutor,
                    rollbackService);

            InvalidOperationException exception =
                await Assert.ThrowsAsync<
                    InvalidOperationException>(
                        () =>
                            coordinator
                                .UpgradeAsync());

            Assert.Contains(
                "đã được phục hồi an toàn",
                exception.Message);

            Assert.True(
                migrationExecutor.WasCalled);

            await AssertProbeTableDoesNotExistAsync(
                databasePath);

            await using HrManagementDbContext verifyContext =
                await factory.CreateDbContextAsync();

            string[] appliedMigrations =
                (await verifyContext.Database
                    .GetAppliedMigrationsAsync())
                .ToArray();

            Assert.Equal(
                previousMigration,
                appliedMigrations.LastOrDefault());

            string[] pendingMigrations =
                (await verifyContext.Database
                    .GetPendingMigrationsAsync())
                .ToArray();

            Assert.NotEmpty(
                pendingMigrations);

            string backupDirectory =
                Path.Combine(
                    rootDirectory,
                    "Backups",
                    "Upgrades");

            string[] preUpgradeBackups =
                Directory.Exists(
                    backupDirectory)
                    ? Directory.GetFiles(
                        backupDirectory,
                        "PreUpgrade-*.hrbackup")
                    : Array.Empty<string>();

            Assert.Single(
                preUpgradeBackups);
        }
        finally
        {
            SqliteConnection.ClearAllPools();

            DeleteDirectory(
                rootDirectory);
        }
    }

    private static async Task
        AssertProbeTableDoesNotExistAsync(
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
            FROM sqlite_master
            WHERE type = 'table'
              AND name = '__UpgradeFailureProbe';
            """;

        long count =
            Convert.ToInt64(
                await command.ExecuteScalarAsync());

        Assert.Equal(
            0,
            count);
    }

    private static string
        CreateTemporaryDirectory()
    {
        string directory =
            Path.Combine(
                Path.GetTempPath(),
                $"hrmanagement-upgrade-failure-{Guid.NewGuid():N}");

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

    private sealed class
        PartiallyFailingMigrationExecutor
        : IDatabaseMigrationExecutor
    {
        private readonly string
            _databasePath;

        public PartiallyFailingMigrationExecutor(
            string databasePath)
        {
            _databasePath =
                databasePath;
        }

        public bool WasCalled
        {
            get;
            private set;
        }

        public async Task MigrateAsync(
            CancellationToken cancellationToken = default)
        {
            WasCalled =
                true;

            var connectionString =
                new SqliteConnectionStringBuilder
                {
                    DataSource =
                        _databasePath,

                    Mode =
                        SqliteOpenMode.ReadWrite,

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
                """
                CREATE TABLE __UpgradeFailureProbe
                (
                    Id INTEGER NOT NULL PRIMARY KEY
                );
                """;

            await command.ExecuteNonQueryAsync(
                cancellationToken);

            throw new InvalidOperationException(
                "Forced migration failure after partial mutation.");
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
