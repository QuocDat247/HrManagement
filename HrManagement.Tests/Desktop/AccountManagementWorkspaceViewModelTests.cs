using HrManagement.Desktop.Services;
using HrManagement.Application.Authentication.Accounts;
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
                new TestConfirmationService());

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
                new TestConfirmationService());

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
                new TestConfirmationService());

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
                new TestConfirmationService());

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
                confirmationService);

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
                });

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
                });

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
        public Guid? LastEditedAccountId
        {
            get;
            private set;
        }

        public bool EditResult
        {
            get;
            set;
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
    }
}
