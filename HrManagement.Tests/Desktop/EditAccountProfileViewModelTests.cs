using HrManagement.Application.Authentication.Accounts;
using HrManagement.Desktop.ViewModels;
using HrManagement.Domain.Authentication.Accounts;
using HrManagement.Domain.Employees;

namespace HrManagement.Tests.Desktop;

public sealed class EditAccountProfileViewModelTests
{
    [Fact]
    public async Task
        LoadAsync_KeepsCurrentEmployeeAndExcludesEmployeesLinkedToOthers()
    {
        Guid accountId =
            Guid.NewGuid();

        Guid otherAccountId =
            Guid.NewGuid();

        Guid currentEmployeeId =
            Guid.NewGuid();

        Guid otherEmployeeId =
            Guid.NewGuid();

        Guid availableEmployeeId =
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
                        currentEmployeeId,
                        "NV001",
                        "Nhân viên hiện tại",
                        Array.Empty<
                            AccountManagementRoleItem>()),

                    new AccountManagementAccountItem(
                        otherAccountId,
                        "other",
                        "Other",
                        UserAccountKind.Standard,
                        true,
                        otherEmployeeId,
                        "NV002",
                        "Nhân viên khác",
                        Array.Empty<
                            AccountManagementRoleItem>())
                },
                Array.Empty<
                    AccountManagementRoleItem>(),
                new[]
                {
                    new AccountManagementEmployeeItem(
                        currentEmployeeId,
                        "NV001",
                        "Nhân viên hiện tại",
                        EmployeeStatus.Active),

                    new AccountManagementEmployeeItem(
                        otherEmployeeId,
                        "NV002",
                        "Nhân viên khác",
                        EmployeeStatus.Active),

                    new AccountManagementEmployeeItem(
                        availableEmployeeId,
                        "NV003",
                        "Nhân viên khả dụng",
                        EmployeeStatus.Active)
                });

        var viewModel =
            new EditAccountProfileViewModel(
                new TestUpdateService(),
                new TestQueryService(
                    snapshot));

        await viewModel.LoadAsync(
            accountId);

        Assert.True(
            viewModel.IsReady);

        Assert.Equal(
            "manager",
            viewModel.Username);

        Assert.Equal(
            "Standard",
            viewModel.KindText);

        Assert.Equal(
            currentEmployeeId,
            viewModel
                .SelectedEmployeeOption
                ?.EmployeeId);

        Assert.Contains(
            viewModel.EmployeeOptions,
            option =>
                option.EmployeeId ==
                currentEmployeeId);

        Assert.Contains(
            viewModel.EmployeeOptions,
            option =>
                option.EmployeeId ==
                availableEmployeeId);

        Assert.DoesNotContain(
            viewModel.EmployeeOptions,
            option =>
                option.EmployeeId ==
                otherEmployeeId);
    }

    [Fact]
    public async Task
        SaveAsync_ForwardsEditableProfileFields()
    {
        Guid accountId =
            Guid.NewGuid();

        Guid employeeId =
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
                new[]
                {
                    new AccountManagementEmployeeItem(
                        employeeId,
                        "NV001",
                        "Nhân viên một",
                        EmployeeStatus.Active)
                });

        var updateService =
            new TestUpdateService();

        var viewModel =
            new EditAccountProfileViewModel(
                updateService,
                new TestQueryService(
                    snapshot));

        await viewModel.LoadAsync(
            accountId);

        viewModel.DisplayName =
            "Tên mới";

        viewModel.SelectedEmployeeOption =
            Assert.Single(
                viewModel.EmployeeOptions,
                option =>
                    option.EmployeeId ==
                    employeeId);

        bool updated =
            false;

        viewModel.AccountUpdated +=
            (_, _) =>
                updated =
                    true;

        await viewModel.SaveCommand
            .ExecuteAsync(
                null);

        Assert.True(
            updated);

        Assert.True(
            updateService.Called);

        Assert.NotNull(
            updateService.LastRequest);

        Assert.Equal(
            accountId,
            updateService
                .LastRequest!
                .AccountId);

        Assert.Equal(
            "Tên mới",
            updateService
                .LastRequest!
                .DisplayName);

        Assert.Equal(
            employeeId,
            updateService
                .LastRequest!
                .EmployeeId);
    }

    [Fact]
    public async Task
        LoadAsync_WhenAccountDoesNotExist_FailsClosed()
    {
        var viewModel =
            new EditAccountProfileViewModel(
                new TestUpdateService(),
                new TestQueryService(
                    new AccountManagementSnapshot(
                        Array.Empty<
                            AccountManagementAccountItem>(),
                        Array.Empty<
                            AccountManagementRoleItem>(),
                        Array.Empty<
                            AccountManagementEmployeeItem>())));

        await viewModel.LoadAsync(
            Guid.NewGuid());

        Assert.False(
            viewModel.IsReady);

        Assert.False(
            viewModel.SaveCommand
                .CanExecute(
                    null));

        Assert.False(
            string.IsNullOrWhiteSpace(
                viewModel.ErrorMessage));
    }

    private sealed class TestUpdateService
        : IAccountProfileUpdateService
    {
        public bool Called
        {
            get;
            private set;
        }

        public UpdateAccountProfileRequest?
            LastRequest
        {
            get;
            private set;
        }

        public UpdateAccountProfileResult Result
        {
            get;
            set;
        } =
            new(
                true);

        public Task<UpdateAccountProfileResult>
            UpdateAsync(
                UpdateAccountProfileRequest request,
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
}
