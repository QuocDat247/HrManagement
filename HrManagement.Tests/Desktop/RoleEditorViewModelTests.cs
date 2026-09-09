using HrManagement.Application.Authentication.Accounts;
using HrManagement.Application.Authorization.Roles;
using HrManagement.Desktop.ViewModels;

namespace HrManagement.Tests.Desktop;

public sealed class RoleEditorViewModelTests
{
    [Fact]
    public async Task
        SaveCommand_InCreateMode_CreatesRole()
    {
        var service =
            new TestRoleManagementService();

        var viewModel =
            new RoleEditorViewModel(
                service);

        viewModel.LoadForCreate();

        viewModel.Name =
            "Quản lý nhân sự";

        viewModel.Description =
            "Quản lý hồ sơ nhân viên.";

        bool saved =
            false;

        viewModel.RoleSaved +=
            (_, _) =>
                saved =
                    true;

        await viewModel.SaveCommand
            .ExecuteAsync(
                null);

        Assert.True(
            saved);

        Assert.True(
            service.CreateCalled);

        Assert.Equal(
            "Quản lý nhân sự",
            service.LastName);

        Assert.Equal(
            "Quản lý hồ sơ nhân viên.",
            service.LastDescription);

        Assert.False(
            service.UpdateCalled);
    }

    [Fact]
    public async Task
        SaveCommand_InEditMode_UpdatesSelectedRole()
    {
        Guid roleId =
            Guid.NewGuid();

        var service =
            new TestRoleManagementService();

        var viewModel =
            new RoleEditorViewModel(
                service);

        viewModel.LoadForEdit(
            new AccountManagementRoleItem(
                roleId,
                "Tên cũ",
                "Mô tả cũ",
                true));

        Assert.True(
            viewModel.IsEditMode);

        viewModel.Name =
            "Tên mới";

        viewModel.Description =
            "Mô tả mới";

        await viewModel.SaveCommand
            .ExecuteAsync(
                null);

        Assert.True(
            service.UpdateCalled);

        Assert.Equal(
            roleId,
            service.LastRoleId);

        Assert.Equal(
            "Tên mới",
            service.LastName);

        Assert.Equal(
            "Mô tả mới",
            service.LastDescription);
    }

    [Fact]
    public async Task
        SaveCommand_WhenServiceRejects_ShowsSafeError()
    {
        var service =
            new TestRoleManagementService
            {
                CreateResult =
                    new RoleManagementResult(
                        false,
                        ErrorMessage:
                            "Tên vai trò đã tồn tại.")
            };

        var viewModel =
            new RoleEditorViewModel(
                service);

        viewModel.LoadForCreate();

        viewModel.Name =
            "Trùng tên";

        bool saved =
            false;

        viewModel.RoleSaved +=
            (_, _) =>
                saved =
                    true;

        await viewModel.SaveCommand
            .ExecuteAsync(
                null);

        Assert.False(
            saved);

        Assert.Equal(
            "Tên vai trò đã tồn tại.",
            viewModel.ErrorMessage);
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

        public Guid? LastRoleId
        {
            get;
            private set;
        }

        public string? LastName
        {
            get;
            private set;
        }

        public string? LastDescription
        {
            get;
            private set;
        }

        public RoleManagementResult CreateResult
        {
            get;
            set;
        } =
            new(
                true,
                Guid.NewGuid());

        public RoleManagementResult UpdateResult
        {
            get;
            set;
        } =
            new(
                true,
                Guid.NewGuid());

        public Task<RoleManagementResult> CreateAsync(
            string name,
            string? description = null,
            CancellationToken cancellationToken = default)
        {
            CreateCalled =
                true;

            LastName =
                name;

            LastDescription =
                description;

            return Task.FromResult(
                CreateResult);
        }

        public Task<RoleManagementResult> UpdateAsync(
            Guid roleId,
            string name,
            string? description = null,
            CancellationToken cancellationToken = default)
        {
            UpdateCalled =
                true;

            LastRoleId =
                roleId;

            LastName =
                name;

            LastDescription =
                description;

            return Task.FromResult(
                UpdateResult);
        }
    }
}
