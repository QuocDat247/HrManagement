using Microsoft.EntityFrameworkCore;

namespace HrManagement.Infrastructure.Persistence.Upgrades;

public sealed class EfDatabaseMigrationExecutor
    : IDatabaseMigrationExecutor
{
    private readonly IDbContextFactory<
        HrManagementDbContext>
        _dbContextFactory;

    public EfDatabaseMigrationExecutor(
        IDbContextFactory<HrManagementDbContext> dbContextFactory)
    {
        _dbContextFactory =
            dbContextFactory;
    }

    public async Task MigrateAsync(
        CancellationToken cancellationToken = default)
    {
        await using HrManagementDbContext dbContext =
            await _dbContextFactory
                .CreateDbContextAsync(
                    cancellationToken);

        await dbContext.Database
            .MigrateAsync(
                cancellationToken);
    }
}
