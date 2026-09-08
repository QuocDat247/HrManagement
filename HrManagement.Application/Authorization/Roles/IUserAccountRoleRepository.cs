using HrManagement.Domain.Authorization.Roles;

namespace HrManagement.Application.Authorization.Roles;

public interface IUserAccountRoleRepository
{
    Task<IReadOnlyList<UserAccountRole>> GetByAccountIdAsync(
        Guid accountId,
        CancellationToken cancellationToken = default);

    Task ReplaceForAccountAsync(
        Guid accountId,
        IReadOnlyCollection<UserAccountRole> assignments,
        CancellationToken cancellationToken = default);
}
