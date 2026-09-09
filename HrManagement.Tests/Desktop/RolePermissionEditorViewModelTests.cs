using HrManagement.Application.Authorization.Roles;
using HrManagement.Desktop.ViewModels;
using HrManagement.Domain.Authorization.Permissions;

namespace HrManagement.Tests.Desktop;

public sealed class RolePermissionEditorViewModelTests
{
    [Fact]
    public async Task
        LoadAsync_MapsAvailableAndAssignedPermissions()
    {
        Guid roleId =
            Guid.NewGuid();

        var snapshot =
            new RolePermissionManagementSnapshot(
                roleId,
                "Quản lý nhân sự",
                true,
                new[]
                {
                    PermissionCodes.AccountView,
                    PermissionCodes.EmployeeEdit,
                    PermissionCodes.EmployeeView
                },
                new[]
                {
                    PermissionCodes.EmployeeView
                });

        var viewModel =
            new RolePermissionEditorViewModel(
                new TestQueryService(
                    snapshot),
                new TestAssignmentService());

        await viewModel.LoadAsync(
            roleId);

        Assert.True(
            viewModel.IsReady);

        Assert.Equal(
            "Quản lý nhân sự",
            viewModel.RoleName);

        Assert.Equal(
            "Đang sử dụng",
            viewModel.RoleStatusText);

        Assert.Equal(
            3,
            viewModel.Permissions.Count);

        Assert.True(
            Assert.Single(
                viewModel.Permissions,
                item =>
                    item.Code ==
                    PermissionCodes.EmployeeView)
                .IsSelected);

        Assert.False(
            Assert.Single(
                viewModel.Permissions,
                item =>
                    item.Code ==
                    PermissionCodes.EmployeeEdit)
                .IsSelected);
    }

    [Fact]
    public async Task
        SaveAsync_ForwardsCurrentlySelectedPermissions()
    {
        Guid roleId =
            Guid.NewGuid();

        var snapshot =
            new RolePermissionManagementSnapshot(
                roleId,
                "Quản lý nhân sự",
                false,
                new[]
                {
                    PermissionCodes.AccountView,
                    PermissionCodes.DepartmentView,
                    PermissionCodes.EmployeeView
                },
                new[]
                {
                    PermissionCodes.AccountView
                });

        var assignmentService =
            new TestAssignmentService();

        var viewModel =
            new RolePermissionEditorViewModel(
                new TestQueryService(
                    snapshot),
                assignmentService);

        await viewModel.LoadAsync(
            roleId);

        Assert.Equal(
            "Ngừng sử dụng",
            viewModel.RoleStatusText);

        RolePermissionSelectionItem accountView =
            Assert.Single(
                viewModel.Permissions,
                item =>
                    item.Code ==
                    PermissionCodes.AccountView);

        RolePermissionSelectionItem employeeView =
            Assert.Single(
                viewModel.Permissions,
                item =>
                    item.Code ==
                    PermissionCodes.EmployeeView);

        accountView.IsSelected =
            false;

        employeeView.IsSelected =
            true;

        bool saved =
            false;

        viewModel.PermissionsSaved +=
            (_, _) =>
                saved =
                    true;

        await viewModel.SaveCommand
            .ExecuteAsync(
                null);

        Assert.True(
            saved);

        Assert.True(
            assignmentService.Called);

        Assert.Equal(
            roleId,
            assignmentService.LastRoleId);

        Assert.Equal(
            new[]
            {
                PermissionCodes.EmployeeView
            },
            assignmentService
                .LastPermissionCodes);
    }

    [Fact]
    public async Task
        LoadAsync_WhenRoleDoesNotExist_FailsClosed()
    {
        var viewModel =
            new RolePermissionEditorViewModel(
                new TestQueryService(
                    null),
                new TestAssignmentService());

        await viewModel.LoadAsync(
            Guid.NewGuid());

        Assert.False(
            viewModel.IsReady);

        Assert.Empty(
            viewModel.Permissions);

        Assert.False(
            viewModel.SaveCommand
                .CanExecute(
                    null));

        Assert.False(
            string.IsNullOrWhiteSpace(
                viewModel.ErrorMessage));
    }

    [Fact]
    public async Task
        SaveAsync_WhenAssignmentRejected_ShowsError()
    {
        Guid roleId =
            Guid.NewGuid();

        var snapshot =
            new RolePermissionManagementSnapshot(
                roleId,
                "Quản lý nhân sự",
                true,
                new[]
                {
                    PermissionCodes.EmployeeView
                },
                Array.Empty<string>());

        var assignmentService =
            new TestAssignmentService
            {
                Result =
                    new RoleManagementResult(
                        false,
                        ErrorMessage:
                            "Không thể cấp cho vai trò quyền vượt quá phạm vi quyền hiện có.")
            };

        var viewModel =
            new RolePermissionEditorViewModel(
                new TestQueryService(
                    snapshot),
                assignmentService);

        await viewModel.LoadAsync(
            roleId);

        Assert.Single(
            viewModel.Permissions)
            .IsSelected =
                true;

        await viewModel.SaveCommand
            .ExecuteAsync(
                null);

        Assert.Equal(
            "Không thể cấp cho vai trò quyền vượt quá phạm vi quyền hiện có.",
            viewModel.ErrorMessage);
    }

    private sealed class TestQueryService
        : IRolePermissionManagementQueryService
    {
        private readonly RolePermissionManagementSnapshot?
            _snapshot;

        public TestQueryService(
            RolePermissionManagementSnapshot? snapshot)
        {
            _snapshot =
                snapshot;
        }

        public Task<RolePermissionManagementSnapshot?> GetAsync(
            Guid roleId,
            CancellationToken cancellationToken = default)
        {
            cancellationToken
                .ThrowIfCancellationRequested();

            return Task.FromResult(
                _snapshot);
        }
    }

    private sealed class TestAssignmentService
        : IRolePermissionAssignmentService
    {
        public bool Called
        {
            get;
            private set;
        }

        public Guid? LastRoleId
        {
            get;
            private set;
        }

        public IReadOnlyList<string>
            LastPermissionCodes
        {
            get;
            private set;
        } =
            Array.Empty<string>();

        public RoleManagementResult Result
        {
            get;
            set;
        } =
            new(
                true,
                Guid.NewGuid());

        public Task<RoleManagementResult> ReplaceAsync(
            Guid roleId,
            IReadOnlyCollection<string> permissionCodes,
            CancellationToken cancellationToken = default)
        {
            cancellationToken
                .ThrowIfCancellationRequested();

            Called =
                true;

            LastRoleId =
                roleId;

            LastPermissionCodes =
                permissionCodes
                    .ToArray();

            return Task.FromResult(
                Result);
        }
    }
}
