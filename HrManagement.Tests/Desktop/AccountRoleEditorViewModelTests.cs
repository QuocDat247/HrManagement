using HrManagement.Application.Authentication.Accounts;
using HrManagement.Application.Authorization.Roles;
using HrManagement.Desktop.ViewModels;
using HrManagement.Domain.Authentication.Accounts;

namespace HrManagement.Tests.Desktop;

public sealed class AccountRoleEditorViewModelTests
{
    [Fact]
    public async Task
        LoadAsync_ForStandardAccount_MapsAssignedRoles()
    {
        Guid accountId =
            Guid.NewGuid();

        Guid activeRoleId =
            Guid.NewGuid();

        Guid inactiveRoleId =
            Guid.NewGuid();

        var activeRole =
            new AccountManagementRoleItem(
                activeRoleId,
                "Quản lý nhân sự",
                "Mô tả",
                true);

        var inactiveRole =
            new AccountManagementRoleItem(
                inactiveRoleId,
                "Vai trò tạm ngừng",
                null,
                false);

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
                        new[]
                        {
                            inactiveRole
                        })
                },
                new[]
                {
                    activeRole,
                    inactiveRole
                },
                Array.Empty<
                    AccountManagementEmployeeItem>());

        var viewModel =
            new AccountRoleEditorViewModel(
                new TestQueryService(
                    snapshot),
                new TestAssignmentService());

        await viewModel.LoadAsync(
            accountId);

        Assert.True(
            viewModel.IsReady);

        Assert.Equal(
            "manager",
            viewModel.Username);

        Assert.Equal(
            "Manager",
            viewModel.DisplayName);

        Assert.Equal(
            2,
            viewModel.Roles.Count);

        Assert.False(
            Assert.Single(
                viewModel.Roles,
                role =>
                    role.RoleId ==
                    activeRoleId)
                .IsSelected);

        AccountRoleSelectionItem inactive =
            Assert.Single(
                viewModel.Roles,
                role =>
                    role.RoleId ==
                    inactiveRoleId);

        Assert.True(
            inactive.IsSelected);

        Assert.Equal(
            "Ngừng sử dụng",
            inactive.StatusText);
    }

    [Fact]
    public async Task
        LoadAsync_ForOwner_FailsClosed()
    {
        Guid accountId =
            Guid.NewGuid();

        var snapshot =
            new AccountManagementSnapshot(
                new[]
                {
                    new AccountManagementAccountItem(
                        accountId,
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
            new AccountRoleEditorViewModel(
                new TestQueryService(
                    snapshot),
                new TestAssignmentService());

        await viewModel.LoadAsync(
            accountId);

        Assert.False(
            viewModel.IsReady);

        Assert.Empty(
            viewModel.Roles);

        Assert.False(
            viewModel.SaveCommand
                .CanExecute(
                    null));

        Assert.Equal(
            "Owner không sử dụng vai trò phân quyền.",
            viewModel.ErrorMessage);
    }

    [Fact]
    public async Task
        SaveAsync_ForwardsSelectedRoleIds()
    {
        Guid accountId =
            Guid.NewGuid();

        Guid firstRoleId =
            Guid.NewGuid();

        Guid secondRoleId =
            Guid.NewGuid();

        var firstRole =
            new AccountManagementRoleItem(
                firstRoleId,
                "Role A",
                null,
                true);

        var secondRole =
            new AccountManagementRoleItem(
                secondRoleId,
                "Role B",
                null,
                false);

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
                        new[]
                        {
                            firstRole
                        })
                },
                new[]
                {
                    firstRole,
                    secondRole
                },
                Array.Empty<
                    AccountManagementEmployeeItem>());

        var assignmentService =
            new TestAssignmentService();

        var viewModel =
            new AccountRoleEditorViewModel(
                new TestQueryService(
                    snapshot),
                assignmentService);

        await viewModel.LoadAsync(
            accountId);

        Assert.Single(
            viewModel.Roles,
            role =>
                role.RoleId ==
                firstRoleId)
            .IsSelected =
                false;

        Assert.Single(
            viewModel.Roles,
            role =>
                role.RoleId ==
                secondRoleId)
            .IsSelected =
                true;

        bool saved =
            false;

        viewModel.RolesSaved +=
            (_, _) =>
                saved =
                    true;

        await viewModel.SaveCommand
            .ExecuteAsync(
                null);

        Assert.True(
            saved);

        Assert.NotNull(
            assignmentService.LastRequest);

        Assert.Equal(
            accountId,
            assignmentService
                .LastRequest!
                .AccountId);

        Assert.Equal(
            new[]
            {
                secondRoleId
            },
            assignmentService
                .LastRequest!
                .RoleIds);
    }

    [Fact]
    public async Task
        SaveAsync_WithNoSelectedRoles_ForwardsEmptyList()
    {
        Guid accountId =
            Guid.NewGuid();

        Guid roleId =
            Guid.NewGuid();

        var role =
            new AccountManagementRoleItem(
                roleId,
                "Role A",
                null,
                true);

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

        var assignmentService =
            new TestAssignmentService();

        var viewModel =
            new AccountRoleEditorViewModel(
                new TestQueryService(
                    snapshot),
                assignmentService);

        await viewModel.LoadAsync(
            accountId);

        Assert.Single(
            viewModel.Roles)
            .IsSelected =
                false;

        await viewModel.SaveCommand
            .ExecuteAsync(
                null);

        Assert.NotNull(
            assignmentService.LastRequest);

        Assert.Empty(
            assignmentService
                .LastRequest!
                .RoleIds);
    }

    [Fact]
    public async Task
        SaveAsync_WhenAssignmentRejected_ShowsError()
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

        var assignmentService =
            new TestAssignmentService
            {
                Result =
                    new AccountRoleAssignmentResult(
                        false,
                        ErrorMessage:
                            "Không thể gán vai trò có quyền vượt quá phạm vi quyền hiện có.")
            };

        var viewModel =
            new AccountRoleEditorViewModel(
                new TestQueryService(
                    snapshot),
                assignmentService);

        await viewModel.LoadAsync(
            accountId);

        await viewModel.SaveCommand
            .ExecuteAsync(
                null);

        Assert.Equal(
            "Không thể gán vai trò có quyền vượt quá phạm vi quyền hiện có.",
            viewModel.ErrorMessage);
    }

    private sealed class TestQueryService
        : IAccountManagementQueryService
    {
        private readonly AccountManagementSnapshot
            _snapshot;

        public TestQueryService(
            AccountManagementSnapshot snapshot)
        {
            _snapshot =
                snapshot;
        }

        public Task<AccountManagementSnapshot> GetAsync(
            CancellationToken cancellationToken = default)
        {
            cancellationToken
                .ThrowIfCancellationRequested();

            return Task.FromResult(
                _snapshot);
        }
    }

    private sealed class TestAssignmentService
        : IUserAccountRoleAssignmentService
    {
        public ReplaceAccountRolesRequest?
            LastRequest
        {
            get;
            private set;
        }

        public AccountRoleAssignmentResult Result
        {
            get;
            set;
        } =
            new(
                true,
                Guid.NewGuid());

        public Task<AccountRoleAssignmentResult> ReplaceAsync(
            ReplaceAccountRolesRequest request,
            CancellationToken cancellationToken = default)
        {
            cancellationToken
                .ThrowIfCancellationRequested();

            LastRequest =
                request;

            return Task.FromResult(
                Result);
        }
    }
}
