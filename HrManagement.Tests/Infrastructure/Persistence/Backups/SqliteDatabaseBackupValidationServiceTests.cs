using HrManagement.Application.Persistence.Backups;
using HrManagement.Domain.Employees;
using HrManagement.Infrastructure.Persistence;
using HrManagement.Infrastructure.Persistence.Backups;
using Microsoft.EntityFrameworkCore;

namespace HrManagement.Tests.Infrastructure.Persistence.Backups;

public sealed class
    SqliteDatabaseBackupValidationServiceTests
{
    [Fact]
    public async Task
        ValidateAsync_WithValidPackage_ReturnsValidBackupInfo()
    {
        string databasePath =
            CreateTemporaryPath(
                ".db");

        string backupPath =
            CreateTemporaryPath(
                ".hrbackup");

        try
        {
            var factory =
                new TestDbContextFactory(
                    databasePath);

            await MigrateAndSeedAsync(
                factory);

            var options =
                new DatabaseInitializationOptions(
                    ApplicationDataMode.Production);

            var backupService =
                new SqliteDatabaseBackupService(
                    factory,
                    options,
                    TimeProvider.System);

            await backupService.CreateAsync(
                backupPath);

            var validationService =
                new SqliteDatabaseBackupValidationService(
                    factory,
                    options);

            DatabaseBackupValidationResult result =
                await validationService.ValidateAsync(
                    backupPath);

            Assert.True(
                result.IsValid);

            Assert.NotNull(
                result.BackupInfo);

            Assert.Equal(
                "Production",
                result.BackupInfo!
                    .DataMode);

            Assert.False(
                string.IsNullOrWhiteSpace(
                    result.BackupInfo
                        .LatestAppliedMigration));
        }
        finally
        {
            DeleteDatabaseFiles(
                databasePath);

            DeleteIfExists(
                backupPath);
        }
    }

    [Fact]
    public async Task
        ValidateAsync_WithDifferentDataMode_ReturnsInvalid()
    {
        string databasePath =
            CreateTemporaryPath(
                ".db");

        string backupPath =
            CreateTemporaryPath(
                ".hrbackup");

        try
        {
            var factory =
                new TestDbContextFactory(
                    databasePath);

            await MigrateAndSeedAsync(
                factory);

            var productionOptions =
                new DatabaseInitializationOptions(
                    ApplicationDataMode.Production);

            var backupService =
                new SqliteDatabaseBackupService(
                    factory,
                    productionOptions,
                    TimeProvider.System);

            await backupService.CreateAsync(
                backupPath);

            var demoValidationService =
                new SqliteDatabaseBackupValidationService(
                    factory,
                    new DatabaseInitializationOptions(
                        ApplicationDataMode.Demo));

            DatabaseBackupValidationResult result =
                await demoValidationService
                    .ValidateAsync(
                        backupPath);

            Assert.False(
                result.IsValid);
        }
        finally
        {
            DeleteDatabaseFiles(
                databasePath);

            DeleteIfExists(
                backupPath);
        }
    }

    [Fact]
    public async Task
        ValidateAsync_WithInvalidFile_ReturnsInvalid()
    {
        string backupPath =
            CreateTemporaryPath(
                ".hrbackup");

        string databasePath =
            CreateTemporaryPath(
                ".db");

        try
        {
            await File.WriteAllTextAsync(
                backupPath,
                "not-a-valid-backup-package");

            var factory =
                new TestDbContextFactory(
                    databasePath);

            var validationService =
                new SqliteDatabaseBackupValidationService(
                    factory,
                    new DatabaseInitializationOptions(
                        ApplicationDataMode.Production));

            DatabaseBackupValidationResult result =
                await validationService
                    .ValidateAsync(
                        backupPath);

            Assert.False(
                result.IsValid);

            Assert.False(
                string.IsNullOrWhiteSpace(
                    result.ErrorMessage));
        }
        finally
        {
            DeleteDatabaseFiles(
                databasePath);

            DeleteIfExists(
                backupPath);
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
                "EMP-VALIDATE",
                "Nhân viên Validation",
                "validation@example.com",
                "0901000888",
                new DateOnly(
                    1994,
                    2,
                    2),
                new DateOnly(
                    2024,
                    2,
                    2),
                "Kiểm thử",
                "Nhân viên",
                EmployeeStatus.Active));

        await dbContext.SaveChangesAsync();
    }

    private static string CreateTemporaryPath(
        string extension)
    {
        return Path.Combine(
            Path.GetTempPath(),
            $"hrmanagement-validation-{Guid.NewGuid():N}{extension}");
    }

    private static void DeleteDatabaseFiles(
        string databasePath)
    {
        DeleteIfExists(
            databasePath);

        DeleteIfExists(
            $"{databasePath}-shm");

        DeleteIfExists(
            $"{databasePath}-wal");
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
