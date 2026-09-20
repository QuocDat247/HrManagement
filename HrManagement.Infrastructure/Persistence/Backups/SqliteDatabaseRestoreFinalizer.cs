using HrManagement.Application.Persistence.Backups;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using System.Data;

namespace HrManagement.Infrastructure.Persistence.Backups;

public sealed class SqliteDatabaseRestoreFinalizer
    : IDatabaseRestoreFinalizer
{
    private readonly IDbContextFactory<
        HrManagementDbContext>
        _dbContextFactory;

    public SqliteDatabaseRestoreFinalizer(
        IDbContextFactory<HrManagementDbContext> dbContextFactory)
    {
        _dbContextFactory =
            dbContextFactory;
    }

    public async Task FinalizeAsync(
        CancellationToken cancellationToken = default)
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
}
