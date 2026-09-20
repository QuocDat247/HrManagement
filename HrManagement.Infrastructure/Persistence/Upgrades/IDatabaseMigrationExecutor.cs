namespace HrManagement.Infrastructure.Persistence.Upgrades;

public interface IDatabaseMigrationExecutor
{
    Task MigrateAsync(
        CancellationToken cancellationToken = default);
}
