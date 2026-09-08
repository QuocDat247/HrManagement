using HrManagement.Application.Authentication;
using HrManagement.Application.Authentication.Accounts;
using HrManagement.Application.Authorization.Roles;
using HrManagement.Domain.Authentication.Accounts;
using HrManagement.Domain.Authorization.Permissions;
using HrManagement.Domain.Authorization.Roles;

namespace HrManagement.Application.Authorization;

public sealed class AuthorizationService
    : IAuthorizationService
{
    private readonly ICurrentUserContext
        _currentUserContext;

    private readonly IUserAccountRepository
        _accountRepository;

    private readonly IUserAccountRoleRepository
        _accountRoleRepository;

    private readonly IRoleRepository
        _roleRepository;

    private readonly IRolePermissionRepository
        _rolePermissionRepository;

    public AuthorizationService(
        ICurrentUserContext currentUserContext,
        IUserAccountRepository accountRepository,
        IUserAccountRoleRepository accountRoleRepository,
        IRoleRepository roleRepository,
        IRolePermissionRepository rolePermissionRepository)
    {
        _currentUserContext =
            currentUserContext;

        _accountRepository =
            accountRepository;

        _accountRoleRepository =
            accountRoleRepository;

        _roleRepository =
            roleRepository;

        _rolePermissionRepository =
            rolePermissionRepository;
    }

    public async Task<bool> HasPermissionAsync(
        string permissionCode,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var permission =
            new Permission(
                permissionCode);

        if (!_currentUserContext.IsAuthenticated
            || _currentUserContext.CurrentUser is null)
        {
            return false;
        }

        if (!Guid.TryParse(
                _currentUserContext.CurrentUser.UserId,
                out Guid accountId))
        {
            return false;
        }

        UserAccount? account =
            await _accountRepository
                .GetByIdAsync(
                    accountId,
                    cancellationToken);

        if (account is null
            || !account.IsActive)
        {
            return false;
        }

        if (account.Kind ==
            UserAccountKind.Owner)
        {
            return true;
        }

        IReadOnlyList<UserAccountRole> assignments =
            await _accountRoleRepository
                .GetByAccountIdAsync(
                    accountId,
                    cancellationToken);

        foreach (UserAccountRole assignment
                 in assignments)
        {
            Role? role =
                await _roleRepository
                    .GetByIdAsync(
                        assignment.RoleId,
                        cancellationToken);

            if (role is null
                || !role.IsActive)
            {
                continue;
            }

            IReadOnlyList<RolePermission>
                rolePermissions =
                    await _rolePermissionRepository
                        .GetByRoleIdAsync(
                            role.Id,
                            cancellationToken);

            if (rolePermissions.Any(
                    rolePermission =>
                        string.Equals(
                            rolePermission.PermissionCode,
                            permission.Code,
                            StringComparison.Ordinal)))
            {
                return true;
            }
        }

        return false;
    }
}
