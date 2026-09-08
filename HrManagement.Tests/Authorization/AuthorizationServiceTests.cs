using HrManagement.Application.Authentication;
using HrManagement.Application.Authentication.Accounts;
using HrManagement.Application.Authorization;
using HrManagement.Application.Authorization.Roles;
using HrManagement.Domain.Authentication.Accounts;
using HrManagement.Domain.Authorization.Permissions;
using HrManagement.Domain.Authorization.Roles;

namespace HrManagement.Tests.Authorization;

public sealed class AuthorizationServiceTests
{
    [Fact]
    public async Task
        HasPermissionAsync_WhenUnauthenticated_ReturnsFalse()
    {
        var service =
            CreateService(
                currentUser:
                    null);

        bool result =
            await service.HasPermissionAsync(
                PermissionCodes.EmployeeView);

        Assert.False(
            result);
    }

    [Fact]
    public async Task
        HasPermissionAsync_ForOwner_ReturnsTrueWithoutRole()
    {
        Guid accountId =
            Guid.NewGuid();

        UserAccount account =
            CreateAccount(
                accountId,
                UserAccountKind.Owner);

        var service =
            CreateService(
                CreateAuthenticatedUser(
                    accountId),
                account);

        bool result =
            await service.HasPermissionAsync(
                PermissionCodes.PayrollClose);

        Assert.True(
            result);
    }

    [Fact]
    public async Task
        HasPermissionAsync_ForStandardAccountWithPermission_ReturnsTrue()
    {
        Guid accountId =
            Guid.NewGuid();

        Guid roleId =
            Guid.NewGuid();

        UserAccount account =
            CreateAccount(
                accountId,
                UserAccountKind.Standard);

        Role role =
            new(
                roleId,
                "Quản lý nhân sự");

        var service =
            CreateService(
                CreateAuthenticatedUser(
                    accountId),
                account,
                new[]
                {
                    new UserAccountRole(
                        accountId,
                        roleId)
                },
                new[]
                {
                    role
                },
                new[]
                {
                    new RolePermission(
                        roleId,
                        PermissionCodes.EmployeeView)
                });

        bool result =
            await service.HasPermissionAsync(
                PermissionCodes.EmployeeView);

        Assert.True(
            result);
    }

    [Fact]
    public async Task
        HasPermissionAsync_WithoutPermission_ReturnsFalse()
    {
        Guid accountId =
            Guid.NewGuid();

        Guid roleId =
            Guid.NewGuid();

        UserAccount account =
            CreateAccount(
                accountId,
                UserAccountKind.Standard);

        Role role =
            new(
                roleId,
                "Nhân viên nhập liệu");

        var service =
            CreateService(
                CreateAuthenticatedUser(
                    accountId),
                account,
                new[]
                {
                    new UserAccountRole(
                        accountId,
                        roleId)
                },
                new[]
                {
                    role
                },
                new[]
                {
                    new RolePermission(
                        roleId,
                        PermissionCodes.EmployeeView)
                });

        bool result =
            await service.HasPermissionAsync(
                PermissionCodes.PayrollClose);

        Assert.False(
            result);
    }

    [Fact]
    public async Task
        HasPermissionAsync_WithInactiveRole_ReturnsFalse()
    {
        Guid accountId =
            Guid.NewGuid();

        Guid roleId =
            Guid.NewGuid();

        UserAccount account =
            CreateAccount(
                accountId,
                UserAccountKind.Standard);

        Role role =
            new(
                roleId,
                "Vai trò đã khóa",
                isActive:
                    false);

        var service =
            CreateService(
                CreateAuthenticatedUser(
                    accountId),
                account,
                new[]
                {
                    new UserAccountRole(
                        accountId,
                        roleId)
                },
                new[]
                {
                    role
                },
                new[]
                {
                    new RolePermission(
                        roleId,
                        PermissionCodes.EmployeeEdit)
                });

        bool result =
            await service.HasPermissionAsync(
                PermissionCodes.EmployeeEdit);

        Assert.False(
            result);
    }

    [Fact]
    public async Task
        HasPermissionAsync_WithInactiveAccount_ReturnsFalse()
    {
        Guid accountId =
            Guid.NewGuid();

        UserAccount account =
            new(
                accountId,
                "disabled-user",
                "Disabled User",
                UserAccountKind.Owner,
                isActive:
                    false);

        var service =
            CreateService(
                CreateAuthenticatedUser(
                    accountId),
                account);

        bool result =
            await service.HasPermissionAsync(
                PermissionCodes.SettingsManage);

        Assert.False(
            result);
    }

    [Fact]
    public async Task
        HasPermissionAsync_WithUnknownPermission_Throws()
    {
        var service =
            CreateService(
                currentUser:
                    null);

        await Assert.ThrowsAsync<
            ArgumentException>(
                () =>
                    service.HasPermissionAsync(
                        "Unknown.Permission"));
    }

    private static AuthorizationService CreateService(
        AuthenticatedUser? currentUser,
        UserAccount? account = null,
        IReadOnlyList<UserAccountRole>? assignments = null,
        IReadOnlyList<Role>? roles = null,
        IReadOnlyList<RolePermission>? permissions = null)
    {
        return new AuthorizationService(
            new TestCurrentUserContext(
                currentUser),
            new TestUserAccountRepository(
                account),
            new TestUserAccountRoleRepository(
                assignments
                ?? Array.Empty<UserAccountRole>()),
            new TestRoleRepository(
                roles
                ?? Array.Empty<Role>()),
            new TestRolePermissionRepository(
                permissions
                ?? Array.Empty<RolePermission>()));
    }

    private static AuthenticatedUser
        CreateAuthenticatedUser(
            Guid accountId)
    {
        return new AuthenticatedUser(
            accountId.ToString("D"),
            "test-user",
            "Test User");
    }

    private static UserAccount CreateAccount(
        Guid accountId,
        UserAccountKind kind)
    {
        return new UserAccount(
            accountId,
            $"user-{accountId:N}",
            "Test User",
            kind);
    }

    private sealed class TestCurrentUserContext
        : ICurrentUserContext
    {
        public TestCurrentUserContext(
            AuthenticatedUser? currentUser)
        {
            CurrentUser =
                currentUser;
        }

        public AuthenticatedUser? CurrentUser
        {
            get;
        }

        public bool IsAuthenticated =>
            CurrentUser is not null;
    }

    private sealed class TestUserAccountRepository
        : IUserAccountRepository
    {
        private readonly UserAccount?
            _account;

        public TestUserAccountRepository(
            UserAccount? account)
        {
            _account =
                account;
        }

        public Task<UserAccount?> GetByIdAsync(
            Guid id,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(
                _account?.Id == id
                    ? _account
                    : null);
        }

        public Task<IReadOnlyList<UserAccount>>
            GetAllAsync(
                CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task<UserAccount?> GetByUsernameAsync(
            string username,
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task AddAsync(
            UserAccount account,
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task UpdateAsync(
            UserAccount account,
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }
    }

    private sealed class TestUserAccountRoleRepository
        : IUserAccountRoleRepository
    {
        private readonly IReadOnlyList<UserAccountRole>
            _assignments;

        public TestUserAccountRoleRepository(
            IReadOnlyList<UserAccountRole> assignments)
        {
            _assignments =
                assignments;
        }

        public Task<IReadOnlyList<UserAccountRole>>
            GetByAccountIdAsync(
                Guid accountId,
                CancellationToken cancellationToken = default)
        {
            IReadOnlyList<UserAccountRole> result =
                _assignments
                    .Where(assignment =>
                        assignment.AccountId ==
                        accountId)
                    .ToArray();

            return Task.FromResult(
                result);
        }

        public Task ReplaceForAccountAsync(
            Guid accountId,
            IReadOnlyCollection<UserAccountRole> assignments,
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }
    }

    private sealed class TestRoleRepository
        : IRoleRepository
    {
        private readonly IReadOnlyList<Role>
            _roles;

        public TestRoleRepository(
            IReadOnlyList<Role> roles)
        {
            _roles =
                roles;
        }

        public Task<Role?> GetByIdAsync(
            Guid id,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(
                _roles.FirstOrDefault(
                    role =>
                        role.Id == id));
        }

        public Task<IReadOnlyList<Role>> GetAllAsync(
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task<Role?> GetByNameAsync(
            string name,
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task AddAsync(
            Role role,
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task UpdateAsync(
            Role role,
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }
    }

    private sealed class TestRolePermissionRepository
        : IRolePermissionRepository
    {
        private readonly IReadOnlyList<RolePermission>
            _permissions;

        public TestRolePermissionRepository(
            IReadOnlyList<RolePermission> permissions)
        {
            _permissions =
                permissions;
        }

        public Task<IReadOnlyList<RolePermission>>
            GetByRoleIdAsync(
                Guid roleId,
                CancellationToken cancellationToken = default)
        {
            IReadOnlyList<RolePermission> result =
                _permissions
                    .Where(permission =>
                        permission.RoleId ==
                        roleId)
                    .ToArray();

            return Task.FromResult(
                result);
        }

        public Task ReplaceForRoleAsync(
            Guid roleId,
            IReadOnlyCollection<RolePermission> permissions,
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }
    }
}
