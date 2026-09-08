using HrManagement.Application.Authorization.Roles;
using HrManagement.Domain.Authorization.Roles;
using HrManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HrManagement.Infrastructure.Authorization.Roles;

public sealed class EfUserAccountRoleRepository
    : IUserAccountRoleRepository
{
    private readonly IDbContextFactory<HrManagementDbContext>
        _dbContextFactory;

    public EfUserAccountRoleRepository(
        IDbContextFactory<HrManagementDbContext> dbContextFactory)
    {
        _dbContextFactory =
            dbContextFactory;
    }

    public async Task<IReadOnlyList<UserAccountRole>>
        GetByAccountIdAsync(
            Guid accountId,
            CancellationToken cancellationToken = default)
    {
        await using HrManagementDbContext dbContext =
            await _dbContextFactory.CreateDbContextAsync(
                cancellationToken);

        return await dbContext.UserAccountRoles
            .AsNoTracking()
            .Where(assignment =>
                assignment.AccountId ==
                accountId)
            .OrderBy(assignment =>
                assignment.RoleId)
            .ToListAsync(
                cancellationToken);
    }

    public async Task ReplaceForAccountAsync(
        Guid accountId,
        IReadOnlyCollection<UserAccountRole> assignments,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(
            assignments);

        if (assignments.Any(
                assignment =>
                    assignment.AccountId !=
                    accountId))
        {
            throw new ArgumentException(
                "Danh sách vai trò chứa gán vai trò thuộc tài khoản khác.",
                nameof(assignments));
        }

        await using HrManagementDbContext dbContext =
            await _dbContextFactory.CreateDbContextAsync(
                cancellationToken);

        List<UserAccountRole> existing =
            await dbContext.UserAccountRoles
                .Where(assignment =>
                    assignment.AccountId ==
                    accountId)
                .ToListAsync(
                    cancellationToken);

        dbContext.UserAccountRoles.RemoveRange(
            existing);

        await dbContext.UserAccountRoles.AddRangeAsync(
            assignments,
            cancellationToken);

        await dbContext.SaveChangesAsync(
            cancellationToken);
    }
}
