using HrManagement.Application.Authorization.Roles;
using HrManagement.Domain.Authorization.Permissions;
using HrManagement.Domain.Authorization.Roles;

namespace HrManagement.Tests.Authorization;

public sealed class RolePermissionManagementQueryServiceTests
{
    [Fact]
    public async Task
        GetAsync_ReturnsCatalogAndAssignedPermissions()
    {
        Guid roleId =
            Guid.NewGuid();

        var role =
            new Role(
                roleId,
                "Quản lý nhân sự",
                "Mô tả",
                isActive:
                    false);

        var permissionRepository =
            new TestRolePermissionRepository(
                new[]
                {
                    new RolePermission(
                        roleId,
                        PermissionCodes.EmployeeView),

                    new RolePermission(
                        roleId,
                        PermissionCodes.AccountView)
                });

        var service =
            new RolePermissionManagementQueryService(
                new TestRoleRepository(
                    role),
                permissionRepository);

        RolePermissionManagementSnapshot? snapshot =
            await service.GetAsync(
                roleId);

        Assert.NotNull(
            snapshot);

        Assert.Equal(
            roleId,
            snapshot!.RoleId);

        Assert.Equal(
            "Quản lý nhân sự",
            snapshot.RoleName);

        Assert.False(
            snapshot.IsActive);

        Assert.Equal(
            PermissionCodes.All.Count,
            snapshot
                .AvailablePermissionCodes
                .Count);

        Assert.Contains(
            PermissionCodes.RoleAssignPermission,
            snapshot.AvailablePermissionCodes);

        Assert.Equal(
            new[]
            {
                PermissionCodes.AccountView,
                PermissionCodes.EmployeeView
            },
            snapshot.AssignedPermissionCodes);
    }

    [Fact]
    public async Task
        GetAsync_WhenRoleDoesNotExist_ReturnsNullWithoutReadingAssignments()
    {
        var permissionRepository =
            new TestRolePermissionRepository(
                Array.Empty<
                    RolePermission>());

        var service =
            new RolePermissionManagementQueryService(
                new TestRoleRepository(
                    null),
                permissionRepository);

        RolePermissionManagementSnapshot? snapshot =
            await service.GetAsync(
                Guid.NewGuid());

        Assert.Null(
            snapshot);

        Assert.False(
            permissionRepository.Called);
    }

    private sealed class TestRoleRepository
        : IRoleRepository
    {
        private readonly Role?
            _role;

        public TestRoleRepository(
            Role? role)
        {
            _role =
                role;
        }

        public Task<IReadOnlyList<Role>> GetAllAsync(
            CancellationToken cancellationToken = default)
        {
            IReadOnlyList<Role> roles =
                _role is null
                    ? Array.Empty<Role>()
                    : new[]
                    {
                        _role
                    };

            return Task.FromResult(
                roles);
        }

        public Task<Role?> GetByIdAsync(
            Guid id,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<Role?>(
                _role?.Id ==
                    id
                    ? _role
                    : null);
        }

        public Task<Role?> GetByNameAsync(
            string name,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<Role?>(
                _role is not null
                && string.Equals(
                    _role.Name,
                    name,
                    StringComparison.OrdinalIgnoreCase)
                    ? _role
                    : null);
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

        public bool Called
        {
            get;
            private set;
        }

        public Task<IReadOnlyList<RolePermission>>
            GetByRoleIdAsync(
                Guid roleId,
                CancellationToken cancellationToken = default)
        {
            Called =
                true;

            IReadOnlyList<RolePermission> result =
                _permissions
                    .Where(
                        permission =>
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
