using HrManagement.Application.Authorization.Roles;
using HrManagement.Application.Authentication.Accounts;
using HrManagement.Desktop.Services;
using HrManagement.Desktop.ViewModels;
using HrManagement.Domain.Authentication.Accounts;

namespace HrManagement.Tests.Desktop;

public sealed class AccountManagementWorkspaceViewModelTests
{
    [Fact]
    public async Task
        LoadAsync_MapsAccountsAndRolesForDisplay()
    {
        Guid accountId =
            Guid.NewGuid();

        Guid roleId =
            Guid.NewGuid();

        var role =
            new AccountManagementRoleItem(
                roleId,
                "Quản lý nhân sự",
                "Quản lý hồ sơ nhân viên.",
                true);

        var snapshot =
            new AccountManagementSnapshot(
                new[]
                {
                    new AccountManagementAccountItem(
                        accountId,
                        "hr.manager",
                        "Quản lý nhân sự",
                        UserAccountKind.Standard,
                        true,
                        null,
                        null,
                        null,
                        new[]
                        {
                            role
                        })
                },
                new[]
                {
                    role
                },
                Array.Empty<
                    AccountManagementEmployeeItem>());

        var viewModel =
            new AccountManagementWorkspaceViewModel(
                new TestQueryService(
                    snapshot),
                new TestDialogService(),
                new TestActiveStateService(),
                new TestConfirmationService(),
                new TestRoleActiveStateService());

        await viewModel.LoadAsync();

        Assert.Null(
            viewModel.ErrorMessage);

        AccountManagementAccountRow row =
            Assert.Single(
                viewModel.AccountRows);

        Assert.Equal(
            accountId,
            row.AccountId);

        Assert.Equal(
            "hr.manager",
            row.Username);

        Assert.Equal(
            "Standard",
            row.KindText);

        Assert.Equal(
            "Chưa liên kết",
            row.EmployeeText);

        Assert.Equal(
            "Quản lý nhân sự",
            row.RolesText);

        Assert.Equal(
            "Đang hoạt động",
            row.StatusText);

        Assert.Single(
            viewModel.Roles);
    }

    [Fact]
    public async Task
        LoadAsync_ForOwner_ShowsRoleAsNotApplicable()
    {
        var snapshot =
            new AccountManagementSnapshot(
                new[]
                {
                    new AccountManagementAccountItem(
                        Guid.NewGuid(),
                        "owner",
                        "Chủ doanh nghiệp",
                        UserAccountKind.Owner,
                        true,
                        null,
                        null,
                        null,
                        Array.Empty<
                            AccountManagementRoleItem>())
                },
                Array.Empty<
                    AccountManagementRoleItem>(),
                Array.Empty<
                    AccountManagementEmployeeItem>());

        var viewModel =
            new AccountManagementWorkspaceViewModel(
                new TestQueryService(
                    snapshot),
                new TestDialogService(),
                new TestActiveStateService(),
                new TestConfirmationService(),
                new TestRoleActiveStateService());

        await viewModel.LoadAsync();

        AccountManagementAccountRow row =
            Assert.Single(
                viewModel.AccountRows);

        Assert.Equal(
            "Owner",
            row.KindText);

        Assert.Equal(
            "Không áp dụng (Owner)",
            row.RolesText);
    }

    [Fact]
    public async Task
        LoadAsync_WhenQueryFails_ShowsSafeError()
    {
        var viewModel =
            new AccountManagementWorkspaceViewModel(
                new TestQueryService(
                    exception:
                        new InvalidOperationException(
                            "Database failure.")),
                new TestDialogService(),
                new TestActiveStateService(),
                new TestConfirmationService(),
                new TestRoleActiveStateService());

        await viewModel.LoadAsync();

        Assert.Empty(
            viewModel.AccountRows);

        Assert.Empty(
            viewModel.Roles);

        Assert.False(
            string.IsNullOrWhiteSpace(
                viewModel.ErrorMessage));
    }

    [Fact]
    public async Task
        EditAccountCommand_WithSelection_PassesSelectedAccountId()
    {
        Guid accountId =
            Guid.NewGuid();

        var snapshot =
            new AccountManagementSnapshot(
                new[]
                {
                    new AccountManagementAccountItem(
                        accountId,
                        "manager",
                        "Manager",
                        UserAccountKind.Standard,
                        true,
                        null,
                        null,
                        null,
                        Array.Empty<
                            AccountManagementRoleItem>())
                },
                Array.Empty<
                    AccountManagementRoleItem>(),
                Array.Empty<
                    AccountManagementEmployeeItem>());

        var dialogService =
            new TestDialogService();

        var viewModel =
            new AccountManagementWorkspaceViewModel(
                new TestQueryService(
                    snapshot),
                dialogService,
                new TestActiveStateService(),
                new TestConfirmationService(),
                new TestRoleActiveStateService());

        await viewModel.LoadAsync();

        viewModel.SelectedAccountRow =
            Assert.Single(
                viewModel.AccountRows);

        Assert.True(
            viewModel.EditAccountCommand
                .CanExecute(
                    null));

        await viewModel.EditAccountCommand
            .ExecuteAsync(
                null);

        Assert.Equal(
            accountId,
            dialogService.LastEditedAccountId);
    }

    [Fact]
    public async Task
        DeactivateAccountCommand_WithActiveSelection_ConfirmsAndDisables()
    {
        Guid accountId =
            Guid.NewGuid();

        var snapshot =
            new AccountManagementSnapshot(
                new[]
                {
                    new AccountManagementAccountItem(
                        accountId,
                        "manager",
                        "Manager",
                        UserAccountKind.Standard,
                        true,
                        null,
                        null,
                        null,
                        Array.Empty<
                            AccountManagementRoleItem>())
                },
                Array.Empty<
                    AccountManagementRoleItem>(),
                Array.Empty<
                    AccountManagementEmployeeItem>());

        var activeStateService =
            new TestActiveStateService();

        var confirmationService =
            new TestConfirmationService
            {
                Result =
                    true
            };

        var viewModel =
            new AccountManagementWorkspaceViewModel(
                new TestQueryService(
                    snapshot),
                new TestDialogService(),
                activeStateService,
                confirmationService,
                new TestRoleActiveStateService());

        await viewModel.LoadAsync();

        viewModel.SelectedAccountRow =
            Assert.Single(
                viewModel.AccountRows);

        Assert.True(
            viewModel.DeactivateAccountCommand
                .CanExecute(
                    null));

        Assert.False(
            viewModel.ReactivateAccountCommand
                .CanExecute(
                    null));

        await viewModel.DeactivateAccountCommand
            .ExecuteAsync(
                null);

        Assert.True(
            confirmationService.Called);

        Assert.True(
            activeStateService.Called);

        Assert.NotNull(
            activeStateService.LastRequest);

        Assert.Equal(
            accountId,
            activeStateService
                .LastRequest!
                .AccountId);

        Assert.False(
            activeStateService
                .LastRequest!
                .IsActive);
    }

    [Fact]
    public async Task
        DeactivateAccountCommand_WhenConfirmationIsDeclined_DoesNotCallService()
    {
        var snapshot =
            new AccountManagementSnapshot(
                new[]
                {
                    new AccountManagementAccountItem(
                        Guid.NewGuid(),
                        "manager",
                        "Manager",
                        UserAccountKind.Standard,
                        true,
                        null,
                        null,
                        null,
                        Array.Empty<
                            AccountManagementRoleItem>())
                },
                Array.Empty<
                    AccountManagementRoleItem>(),
                Array.Empty<
                    AccountManagementEmployeeItem>());

        var activeStateService =
            new TestActiveStateService();

        var viewModel =
            new AccountManagementWorkspaceViewModel(
                new TestQueryService(
                    snapshot),
                new TestDialogService(),
                activeStateService,
                new TestConfirmationService
                {
                    Result =
                        false
                },
                new TestRoleActiveStateService());

        await viewModel.LoadAsync();

        viewModel.SelectedAccountRow =
            Assert.Single(
                viewModel.AccountRows);

        await viewModel.DeactivateAccountCommand
            .ExecuteAsync(
                null);

        Assert.False(
            activeStateService.Called);
    }

    [Fact]
    public async Task
        ReactivateAccountCommand_WithInactiveSelection_RequestsActiveState()
    {
        Guid accountId =
            Guid.NewGuid();

        var snapshot =
            new AccountManagementSnapshot(
                new[]
                {
                    new AccountManagementAccountItem(
                        accountId,
                        "manager",
                        "Manager",
                        UserAccountKind.Standard,
                        false,
                        null,
                        null,
                        null,
                        Array.Empty<
                            AccountManagementRoleItem>())
                },
                Array.Empty<
                    AccountManagementRoleItem>(),
                Array.Empty<
                    AccountManagementEmployeeItem>());

        var activeStateService =
            new TestActiveStateService();

        var viewModel =
            new AccountManagementWorkspaceViewModel(
                new TestQueryService(
                    snapshot),
                new TestDialogService(),
                activeStateService,
                new TestConfirmationService
                {
                    Result =
                        true
                },
                new TestRoleActiveStateService());

        await viewModel.LoadAsync();

        viewModel.SelectedAccountRow =
            Assert.Single(
                viewModel.AccountRows);

        Assert.False(
            viewModel.DeactivateAccountCommand
                .CanExecute(
                    null));

        Assert.True(
            viewModel.ReactivateAccountCommand
                .CanExecute(
                    null));

        await viewModel.ReactivateAccountCommand
            .ExecuteAsync(
                null);

        Assert.True(
            activeStateService.Called);

        Assert.True(
            activeStateService
                .LastRequest!
                .IsActive);
    }

    [Fact]
    public async Task
        CreateRoleCommand_OpensCreateRoleDialog()
    {
        var snapshot =
            new AccountManagementSnapshot(
                Array.Empty<
                    AccountManagementAccountItem>(),
                Array.Empty<
                    AccountManagementRoleItem>(),
                Array.Empty<
                    AccountManagementEmployeeItem>());

        var dialogService =
            new TestDialogService();

        var viewModel =
            new AccountManagementWorkspaceViewModel(
                new TestQueryService(
                    snapshot),
                dialogService,
                new TestActiveStateService(),
                new TestConfirmationService(),
                new TestRoleActiveStateService());

        await viewModel.LoadAsync();

        Assert.True(
            viewModel.CreateRoleCommand
                .CanExecute(
                    null));

        await viewModel.CreateRoleCommand
            .ExecuteAsync(
                null);

        Assert.True(
            dialogService.CreateRoleCalled);
    }

    [Fact]
    public async Task
        EditRoleCommand_WithSelection_PassesSelectedRole()
    {
        Guid roleId =
            Guid.NewGuid();

        var role =
            new AccountManagementRoleItem(
                roleId,
                "Quản lý nhân sự",
                "Mô tả",
                true);

        var snapshot =
            new AccountManagementSnapshot(
                Array.Empty<
                    AccountManagementAccountItem>(),
                new[]
                {
                    role
                },
                Array.Empty<
                    AccountManagementEmployeeItem>());

        var dialogService =
            new TestDialogService();

        var viewModel =
            new AccountManagementWorkspaceViewModel(
                new TestQueryService(
                    snapshot),
                dialogService,
                new TestActiveStateService(),
                new TestConfirmationService(),
                new TestRoleActiveStateService());

        await viewModel.LoadAsync();

        Assert.False(
            viewModel.EditRoleCommand
                .CanExecute(
                    null));

        viewModel.SelectedRole =
            Assert.Single(
                viewModel.Roles);

        Assert.True(
            viewModel.EditRoleCommand
                .CanExecute(
                    null));

        await viewModel.EditRoleCommand
            .ExecuteAsync(
                null);

        Assert.Equal(
            roleId,
            dialogService.LastEditedRoleId);
    }

    [Fact]
    public async Task
        DeactivateRoleCommand_WithActiveRole_RequestsInactiveState()
    {
        Guid roleId =
            Guid.NewGuid();

        var role =
            new AccountManagementRoleItem(
                roleId,
                "Quản lý nhân sự",
                "Mô tả",
                true);

        var snapshot =
            new AccountManagementSnapshot(
                Array.Empty<
                    AccountManagementAccountItem>(),
                new[]
                {
                    role
                },
                Array.Empty<
                    AccountManagementEmployeeItem>());

        var roleActiveStateService =
            new TestRoleActiveStateService();

        var confirmationService =
            new TestConfirmationService
            {
                Result =
                    true
            };

        var viewModel =
            new AccountManagementWorkspaceViewModel(
                new TestQueryService(
                    snapshot),
                new TestDialogService(),
                new TestActiveStateService(),
                confirmationService,
                roleActiveStateService);

        await viewModel.LoadAsync();

        viewModel.SelectedRole =
            Assert.Single(
                viewModel.Roles);

        Assert.True(
            viewModel.DeactivateRoleCommand
                .CanExecute(
                    null));

        Assert.False(
            viewModel.ReactivateRoleCommand
                .CanExecute(
                    null));

        await viewModel.DeactivateRoleCommand
            .ExecuteAsync(
                null);

        Assert.True(
            confirmationService.Called);

        Assert.True(
            roleActiveStateService.Called);

        Assert.Equal(
            roleId,
            roleActiveStateService.LastRoleId);

        Assert.False(
            roleActiveStateService.LastIsActive);
    }

    [Fact]
    public async Task
        DeactivateRoleCommand_WhenConfirmationDeclined_DoesNotCallService()
    {
        var role =
            new AccountManagementRoleItem(
                Guid.NewGuid(),
                "Quản lý nhân sự",
                null,
                true);

        var snapshot =
            new AccountManagementSnapshot(
                Array.Empty<
                    AccountManagementAccountItem>(),
                new[]
                {
                    role
                },
                Array.Empty<
                    AccountManagementEmployeeItem>());

        var roleActiveStateService =
            new TestRoleActiveStateService();

        var viewModel =
            new AccountManagementWorkspaceViewModel(
                new TestQueryService(
                    snapshot),
                new TestDialogService(),
                new TestActiveStateService(),
                new TestConfirmationService
                {
                    Result =
                        false
                },
                roleActiveStateService);

        await viewModel.LoadAsync();

        viewModel.SelectedRole =
            Assert.Single(
                viewModel.Roles);

        await viewModel.DeactivateRoleCommand
            .ExecuteAsync(
                null);

        Assert.False(
            roleActiveStateService.Called);
    }

    [Fact]
    public async Task
        ReactivateRoleCommand_WithInactiveRole_RequestsActiveState()
    {
        Guid roleId =
            Guid.NewGuid();

        var role =
            new AccountManagementRoleItem(
                roleId,
                "Quản lý nhân sự",
                null,
                false);

        var snapshot =
            new AccountManagementSnapshot(
                Array.Empty<
                    AccountManagementAccountItem>(),
                new[]
                {
                    role
                },
                Array.Empty<
                    AccountManagementEmployeeItem>());

        var roleActiveStateService =
            new TestRoleActiveStateService();

        var viewModel =
            new AccountManagementWorkspaceViewModel(
                new TestQueryService(
                    snapshot),
                new TestDialogService(),
                new TestActiveStateService(),
                new TestConfirmationService
                {
                    Result =
                        true
                },
                roleActiveStateService);

        await viewModel.LoadAsync();

        viewModel.SelectedRole =
            Assert.Single(
                viewModel.Roles);

        Assert.False(
            viewModel.DeactivateRoleCommand
                .CanExecute(
                    null));

        Assert.True(
            viewModel.ReactivateRoleCommand
                .CanExecute(
                    null));

        await viewModel.ReactivateRoleCommand
            .ExecuteAsync(
                null);

        Assert.True(
            roleActiveStateService.Called);

        Assert.Equal(
            roleId,
            roleActiveStateService.LastRoleId);

        Assert.True(
            roleActiveStateService.LastIsActive);
    }

    [Fact]
    public async Task
        ManageRolePermissionsCommand_WithSelection_PassesRoleId()
    {
        Guid roleId =
            Guid.NewGuid();

        var role =
            new AccountManagementRoleItem(
                roleId,
                "Quản lý nhân sự",
                "Mô tả",
                true);

        var snapshot =
            new AccountManagementSnapshot(
                Array.Empty<
                    AccountManagementAccountItem>(),
                new[]
                {
                    role
                },
                Array.Empty<
                    AccountManagementEmployeeItem>());

        var dialogService =
            new TestDialogService();

        var viewModel =
            new AccountManagementWorkspaceViewModel(
                new TestQueryService(
                    snapshot),
                dialogService,
                new TestActiveStateService(),
                new TestConfirmationService(),
                new TestRoleActiveStateService());

        await viewModel.LoadAsync();

        Assert.False(
            viewModel.ManageRolePermissionsCommand
                .CanExecute(
                    null));

        viewModel.SelectedRole =
            Assert.Single(
                viewModel.Roles);

        Assert.True(
            viewModel.ManageRolePermissionsCommand
                .CanExecute(
                    null));

        await viewModel
            .ManageRolePermissionsCommand
            .ExecuteAsync(
                null);

        Assert.Equal(
            roleId,
            dialogService
                .LastPermissionRoleId);
    }

    [Fact]
    public async Task
        ManageAccountRolesCommand_WithStandardSelection_PassesAccountId()
    {
        Guid accountId =
            Guid.NewGuid();

        var snapshot =
            new AccountManagementSnapshot(
                new[]
                {
                    new AccountManagementAccountItem(
                        accountId,
                        "manager",
                        "Manager",
                        UserAccountKind.Standard,
                        true,
                        null,
                        null,
                        null,
                        Array.Empty<
                            AccountManagementRoleItem>())
                },
                Array.Empty<
                    AccountManagementRoleItem>(),
                Array.Empty<
                    AccountManagementEmployeeItem>());

        var dialogService =
            new TestDialogService();

        var viewModel =
            new AccountManagementWorkspaceViewModel(
                new TestQueryService(
                    snapshot),
                dialogService,
                new TestActiveStateService(),
                new TestConfirmationService(),
                new TestRoleActiveStateService());

        await viewModel.LoadAsync();

        Assert.False(
            viewModel.ManageAccountRolesCommand
                .CanExecute(
                    null));

        viewModel.SelectedAccountRow =
            Assert.Single(
                viewModel.AccountRows);

        Assert.True(
            viewModel.ManageAccountRolesCommand
                .CanExecute(
                    null));

        await viewModel
            .ManageAccountRolesCommand
            .ExecuteAsync(
                null);

        Assert.Equal(
            accountId,
            dialogService
                .LastAccountRoleAccountId);
    }

    [Fact]
    public async Task
        ManageAccountRolesCommand_WithOwnerSelection_IsDisabled()
    {
        var snapshot =
            new AccountManagementSnapshot(
                new[]
                {
                    new AccountManagementAccountItem(
                        Guid.NewGuid(),
                        "owner",
                        "Chủ doanh nghiệp",
                        UserAccountKind.Owner,
                        true,
                        null,
                        null,
                        null,
                        Array.Empty<
                            AccountManagementRoleItem>())
                },
                Array.Empty<
                    AccountManagementRoleItem>(),
                Array.Empty<
                    AccountManagementEmployeeItem>());

        var viewModel =
            new AccountManagementWorkspaceViewModel(
                new TestQueryService(
                    snapshot),
                new TestDialogService(),
                new TestActiveStateService(),
                new TestConfirmationService(),
                new TestRoleActiveStateService());

        await viewModel.LoadAsync();

        viewModel.SelectedAccountRow =
            Assert.Single(
                viewModel.AccountRows);

        Assert.False(
            viewModel.ManageAccountRolesCommand
                .CanExecute(
                    null));
    }

    private sealed class TestQueryService
        : IAccountManagementQueryService
    {
        private readonly AccountManagementSnapshot?
            _snapshot;

        private readonly Exception?
            _exception;

        public TestQueryService(
            AccountManagementSnapshot snapshot)
        {
            _snapshot =
                snapshot;
        }

        public TestQueryService(
            Exception exception)
        {
            _exception =
                exception;
        }

        public Task<AccountManagementSnapshot> GetAsync(
            CancellationToken cancellationToken = default)
        {
            cancellationToken
                .ThrowIfCancellationRequested();

            if (_exception is not null)
            {
                throw _exception;
            }

            return Task.FromResult(
                _snapshot!);
        }
    }

    private sealed class TestActiveStateService
        : IAccountActiveStateService
    {
        public bool Called
        {
            get;
            private set;
        }

        public SetAccountActiveStateRequest?
            LastRequest
        {
            get;
            private set;
        }

        public SetAccountActiveStateResult Result
        {
            get;
            set;
        } =
            new(
                true);

        public Task<SetAccountActiveStateResult> SetAsync(
            SetAccountActiveStateRequest request,
            CancellationToken cancellationToken = default)
        {
            Called =
                true;

            LastRequest =
                request;

            return Task.FromResult(
                Result);
        }
    }

    private sealed class TestRoleActiveStateService
        : IRoleActiveStateService
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

        public bool LastIsActive
        {
            get;
            private set;
        }

        public RoleManagementResult? Result
        {
            get;
            set;
        }

        public Task<RoleManagementResult> SetAsync(
            Guid roleId,
            bool isActive,
            CancellationToken cancellationToken = default)
        {
            Called =
                true;

            LastRoleId =
                roleId;

            LastIsActive =
                isActive;

            return Task.FromResult(
                Result
                ?? new RoleManagementResult(
                    true,
                    roleId));
        }
    }

    private sealed class TestConfirmationService
        : IUserConfirmationService
    {
        public bool Result
        {
            get;
            set;
        }

        public bool Called
        {
            get;
            private set;
        }

        public bool Confirm(
            string title,
            string message)
        {
            Called =
                true;

            return Result;
        }
    }

    private sealed class TestDialogService
        : HrManagement.Desktop.Services.Accounts
            .IAccountManagementDialogService
    {
        public Guid? LastAccountRoleAccountId
        {
            get;
            private set;
        }

        public Guid? LastPermissionRoleId
        {
            get;
            private set;
        }

        public Guid? LastEditedAccountId
        {
            get;
            private set;
        }

        public Guid? LastEditedRoleId
        {
            get;
            private set;
        }

        public bool EditResult
        {
            get;
            set;
        }

        public bool CreateRoleCalled
        {
            get;
            private set;
        }

        public bool ShowManageAccountRolesDialog(
            Guid accountId)
        {
            LastAccountRoleAccountId =
                accountId;

            return false;
        }

        public bool ShowManageRolePermissionsDialog(
            Guid roleId)
        {
            LastPermissionRoleId =
                roleId;

            return false;
        }

        public bool ShowCreateAccountDialog()
        {
            return false;
        }

        public bool ShowEditAccountDialog(
            Guid accountId)
        {
            LastEditedAccountId =
                accountId;

            return EditResult;
        }

        public bool ShowCreateRoleDialog()
        {
            CreateRoleCalled =
                true;

            return false;
        }

        public bool ShowEditRoleDialog(
            AccountManagementRoleItem role)
        {
            LastEditedRoleId =
                role.RoleId;

            return false;
        }
    }
}
