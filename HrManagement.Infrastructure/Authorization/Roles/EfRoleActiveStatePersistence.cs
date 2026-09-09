using HrManagement.Application.Authorization.Roles;
using HrManagement.Domain.Authorization.Roles;
using HrManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HrManagement.Infrastructure.Authorization.Roles;

public sealed class EfRoleActiveStatePersistence
    : IRoleActiveStatePersistence
{
    private readonly IDbContextFactory<HrManagementDbContext>
        _dbContextFactory;

    public EfRoleActiveStatePersistence(
        IDbContextFactory<HrManagementDbContext> dbContextFactory)
    {
        _dbContextFactory =
            dbContextFactory;
    }

    public async Task<RoleActiveStatePersistenceResult>
        TrySetAsync(
            Guid roleId,
            bool isActive,
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

        Role? role =
            await dbContext
                .Roles
                .SingleOrDefaultAsync(
                    item =>
                        item.Id ==
                        roleId,
                    cancellationToken);

        if (role is null)
        {
            return RoleActiveStatePersistenceResult
                .RoleNotFound;
        }

        if (role.IsActive ==
            isActive)
        {
            return RoleActiveStatePersistenceResult
                .Unchanged;
        }

        role.SetActive(
            isActive);

        await dbContext.SaveChangesAsync(
            cancellationToken);

        return RoleActiveStatePersistenceResult
            .Updated;
    }
}
