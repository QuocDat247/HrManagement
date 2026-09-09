using HrManagement.Application.Authorization;
using HrManagement.Application.Authorization.Roles;
using HrManagement.Domain.Authorization.Permissions;
using HrManagement.Domain.Authorization.Roles;

namespace HrManagement.Tests.Authorization;

public sealed class RoleManagementServiceTests
{
    [Fact]
    public async Task
        CreateAsync_WithValidValues_ForwardsNormalizedRole()
    {
        var persistence =
            new TestPersistence();

        var service =
            new RoleManagementService(
                persistence);

        RoleManagementResult result =
            await service.CreateAsync(
                "  Quản lý nhân sự  ",
                "  Quản lý hồ sơ.  ");

        Assert.True(
            result.IsSuccessful);

        Assert.NotNull(
            persistence.CreatedRole);

        Assert.Equal(
            "Quản lý nhân sự",
            persistence.CreatedRole!.Name);

        Assert.Equal(
            "QUẢN LÝ NHÂN SỰ",
            persistence.CreatedRole.NormalizedName);

        Assert.True(
            persistence.CreatedRole.IsActive);
    }

    [Fact]
    public async Task
        UpdateAsync_WithValidValues_ForwardsNormalizedDetails()
    {
        var persistence =
            new TestPersistence();

        var service =
            new RoleManagementService(
                persistence);

        Guid roleId =
            Guid.NewGuid();

        RoleManagementResult result =
            await service.UpdateAsync(
                roleId,
                "  Kế toán  ",
                "  Xử lý lương.  ");

        Assert.True(
            result.IsSuccessful);

        Assert.Equal(
            roleId,
            persistence.UpdatedRoleId);

        Assert.Equal(
            "Kế toán",
            persistence.UpdatedName);

        Assert.Equal(
            "Xử lý lương.",
            persistence.UpdatedDescription);
    }

    [Fact]
    public async Task
        CreateAsync_WhenNameAlreadyExists_ReturnsFailure()
    {
        var persistence =
            new TestPersistence
            {
                CreateResult =
                    RoleManagementPersistenceResult
                        .NameAlreadyExists
            };

        var service =
            new RoleManagementService(
                persistence);

        RoleManagementResult result =
            await service.CreateAsync(
                "Quản lý");

        Assert.False(
            result.IsSuccessful);
    }

    [Fact]
    public async Task
        AuthorizedCreate_RequiresRoleCreate()
    {
        var guard =
            new TestAuthorizationGuard();

        var inner =
            new TestRoleManagementService();

        var service =
            new AuthorizedRoleManagementService(
                inner,
                guard);

        await service.CreateAsync(
            "Quản lý");

        Assert.Equal(
            PermissionCodes.RoleCreate,
            guard.LastPermissionCode);

        Assert.True(
            inner.CreateCalled);
    }

    [Fact]
    public async Task
        AuthorizedUpdate_RequiresRoleEdit()
    {
        var guard =
            new TestAuthorizationGuard();

        var inner =
            new TestRoleManagementService();

        var service =
            new AuthorizedRoleManagementService(
                inner,
                guard);

        await service.UpdateAsync(
            Guid.NewGuid(),
            "Quản lý");

        Assert.Equal(
            PermissionCodes.RoleEdit,
            guard.LastPermissionCode);

        Assert.True(
            inner.UpdateCalled);
    }

    private sealed class TestPersistence
        : IRoleManagementPersistence
    {
        public RoleManagementPersistenceResult CreateResult
        {
            get;
            set;
        } =
            RoleManagementPersistenceResult.Created;

        public RoleManagementPersistenceResult UpdateResult
        {
            get;
            set;
        } =
            RoleManagementPersistenceResult.Updated;

        public Role? CreatedRole
        {
            get;
            private set;
        }

        public Guid UpdatedRoleId
        {
            get;
            private set;
        }

        public string? UpdatedName
        {
            get;
            private set;
        }

        public string? UpdatedDescription
        {
            get;
            private set;
        }

        public Task<RoleManagementPersistenceResult> TryCreateAsync(
            Role role,
            CancellationToken cancellationToken = default)
        {
            CreatedRole =
                role;

            return Task.FromResult(
                CreateResult);
        }

        public Task<RoleManagementPersistenceResult> TryUpdateAsync(
            Guid roleId,
            string name,
            string? description,
            CancellationToken cancellationToken = default)
        {
            UpdatedRoleId =
                roleId;

            UpdatedName =
                name;

            UpdatedDescription =
                description;

            return Task.FromResult(
                UpdateResult);
        }
    }

    private sealed class TestAuthorizationGuard
        : IAuthorizationGuard
    {
        public string? LastPermissionCode
        {
            get;
            private set;
        }

        public Task RequirePermissionAsync(
            string permissionCode,
            CancellationToken cancellationToken = default)
        {
            LastPermissionCode =
                permissionCode;

            return Task.CompletedTask;
        }
    }

    private sealed class TestRoleManagementService
        : IRoleManagementService
    {
        public bool CreateCalled
        {
            get;
            private set;
        }

        public bool UpdateCalled
        {
            get;
            private set;
        }

        public Task<RoleManagementResult> CreateAsync(
            string name,
            string? description = null,
            CancellationToken cancellationToken = default)
        {
            CreateCalled =
                true;

            return Task.FromResult(
                new RoleManagementResult(
                    true,
                    Guid.NewGuid()));
        }

        public Task<RoleManagementResult> UpdateAsync(
            Guid roleId,
            string name,
            string? description = null,
            CancellationToken cancellationToken = default)
        {
            UpdateCalled =
                true;

            return Task.FromResult(
                new RoleManagementResult(
                    true,
                    roleId));
        }
    }
}
