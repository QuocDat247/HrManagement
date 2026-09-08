using HrManagement.Application.Authorization.Roles;
using HrManagement.Domain.Authorization.Roles;
using HrManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HrManagement.Infrastructure.Authorization.Roles;

public sealed class EfRolePermissionRepository
    : IRolePermissionRepository
{
    private readonly IDbContextFactory<HrManagementDbContext>
        _dbContextFactory;

    public EfRolePermissionRepository(
        IDbContextFactory<HrManagementDbContext> dbContextFactory)
    {
        _dbContextFactory =
            dbContextFactory;
    }

    public async Task<IReadOnlyList<RolePermission>>
        GetByRoleIdAsync(
            Guid roleId,
            CancellationToken cancellationToken = default)
    {
        await using HrManagementDbContext dbContext =
            await _dbContextFactory.CreateDbContextAsync(
                cancellationToken);

        return await dbContext.RolePermissions
            .AsNoTracking()
            .Where(rolePermission =>
                rolePermission.RoleId ==
                roleId)
            .OrderBy(rolePermission =>
                rolePermission.PermissionCode)
            .ToListAsync(
                cancellationToken);
    }

    public async Task ReplaceForRoleAsync(
        Guid roleId,
        IReadOnlyCollection<RolePermission> permissions,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(
            permissions);

        if (permissions.Any(
                permission =>
                    permission.RoleId !=
                    roleId))
        {
            throw new ArgumentException(
                "Danh sách quyền chứa quyền thuộc vai trò khác.",
                nameof(permissions));
        }

        await using HrManagementDbContext dbContext =
            await _dbContextFactory.CreateDbContextAsync(
                cancellationToken);

        List<RolePermission> existing =
            await dbContext.RolePermissions
                .Where(rolePermission =>
                    rolePermission.RoleId ==
                    roleId)
                .ToListAsync(
                    cancellationToken);

        dbContext.RolePermissions.RemoveRange(
            existing);

        await dbContext.RolePermissions.AddRangeAsync(
            permissions,
            cancellationToken);

        await dbContext.SaveChangesAsync(
            cancellationToken);
    }
}
