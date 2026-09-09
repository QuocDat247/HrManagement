using HrManagement.Application.Authorization.Roles;
using HrManagement.Domain.Authorization.Roles;
using HrManagement.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace HrManagement.Infrastructure.Authorization.Roles;

public sealed class EfRoleManagementPersistence
    : IRoleManagementPersistence
{
    private readonly IDbContextFactory<HrManagementDbContext>
        _dbContextFactory;

    public EfRoleManagementPersistence(
        IDbContextFactory<HrManagementDbContext> dbContextFactory)
    {
        _dbContextFactory =
            dbContextFactory;
    }

    public async Task<RoleManagementPersistenceResult>
        TryCreateAsync(
            Role role,
            CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(
            role);

        await using HrManagementDbContext dbContext =
            await _dbContextFactory
                .CreateDbContextAsync(
                    cancellationToken);

        var connection =
            (SqliteConnection)
                dbContext.Database
                    .GetDbConnection();

        await connection.OpenAsync(
            cancellationToken);

        await using SqliteTransaction transaction =
            connection.BeginTransaction(
                deferred:
                    false);

        await dbContext.Database
            .UseTransactionAsync(
                transaction,
                cancellationToken);

        bool nameExists =
            await dbContext
                .Roles
                .AsNoTracking()
                .AnyAsync(
                    existing =>
                        existing.NormalizedName ==
                        role.NormalizedName,
                    cancellationToken);

        if (nameExists)
        {
            await transaction.RollbackAsync(
                cancellationToken);

            return RoleManagementPersistenceResult
                .NameAlreadyExists;
        }

        await dbContext.Roles
            .AddAsync(
                role,
                cancellationToken);

        await dbContext.SaveChangesAsync(
            cancellationToken);

        await transaction.CommitAsync(
            cancellationToken);

        return RoleManagementPersistenceResult
            .Created;
    }

    public async Task<RoleManagementPersistenceResult>
        TryUpdateAsync(
            Guid roleId,
            string name,
            string? description,
            CancellationToken cancellationToken = default)
    {
        if (roleId == Guid.Empty)
        {
            throw new ArgumentException(
                "Mã vai trò không hợp lệ.",
                nameof(roleId));
        }

        await using HrManagementDbContext dbContext =
            await _dbContextFactory
                .CreateDbContextAsync(
                    cancellationToken);

        var connection =
            (SqliteConnection)
                dbContext.Database
                    .GetDbConnection();

        await connection.OpenAsync(
            cancellationToken);

        await using SqliteTransaction transaction =
            connection.BeginTransaction(
                deferred:
                    false);

        await dbContext.Database
            .UseTransactionAsync(
                transaction,
                cancellationToken);

        Role? role =
            await dbContext
                .Roles
                .SingleOrDefaultAsync(
                    existing =>
                        existing.Id ==
                        roleId,
                    cancellationToken);

        if (role is null)
        {
            await transaction.RollbackAsync(
                cancellationToken);

            return RoleManagementPersistenceResult
                .RoleNotFound;
        }

        string normalizedName =
            name.Trim()
                .ToUpperInvariant();

        bool nameExists =
            await dbContext
                .Roles
                .AsNoTracking()
                .AnyAsync(
                    existing =>
                        existing.Id !=
                            roleId
                        && existing.NormalizedName ==
                            normalizedName,
                    cancellationToken);

        if (nameExists)
        {
            await transaction.RollbackAsync(
                cancellationToken);

            return RoleManagementPersistenceResult
                .NameAlreadyExists;
        }

        role.UpdateDetails(
            name,
            description);

        await dbContext.SaveChangesAsync(
            cancellationToken);

        await transaction.CommitAsync(
            cancellationToken);

        return RoleManagementPersistenceResult
            .Updated;
    }
}
