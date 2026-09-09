using HrManagement.Application.Authentication.Accounts;
using HrManagement.Desktop.ViewModels;
using HrManagement.Domain.Authentication.Accounts;
using HrManagement.Domain.Employees;

namespace HrManagement.Tests.Desktop;

public sealed class CreateStandardAccountViewModelTests
{
    [Fact]
    public async Task
        LoadAsync_ShowsOnlyEmployeesNotAlreadyLinked()
    {
        Guid linkedEmployeeId =
            Guid.NewGuid();

        Guid availableEmployeeId =
            Guid.NewGuid();

        var snapshot =
            new AccountManagementSnapshot(
                new[]
                {
                    new AccountManagementAccountItem(
                        Guid.NewGuid(),
                        "existing",
                        "Existing User",
                        UserAccountKind.Standard,
                        true,
                        linkedEmployeeId,
                        "NV001",
                        "Nhân viên đã liên kết",
                        Array.Empty<
                            AccountManagementRoleItem>())
                },
                Array.Empty<
                    AccountManagementRoleItem>(),
                new[]
                {
                    new AccountManagementEmployeeItem(
                        linkedEmployeeId,
                        "NV001",
                        "Nhân viên đã liên kết",
                        EmployeeStatus.Active),

                    new AccountManagementEmployeeItem(
                        availableEmployeeId,
                        "NV002",
                        "Nhân viên chưa liên kết",
                        EmployeeStatus.Active)
                });

        var viewModel =
            new CreateStandardAccountViewModel(
                new TestCreationService(),
                new TestQueryService(
                    snapshot));

        await viewModel.LoadAsync();

        Assert.True(
            viewModel.IsReady);

        Assert.Equal(
            2,
            viewModel.EmployeeOptions.Count);

        Assert.Contains(
            viewModel.EmployeeOptions,
            option =>
                option.EmployeeId is null);

        Assert.Contains(
            viewModel.EmployeeOptions,
            option =>
                option.EmployeeId ==
                availableEmployeeId);

        Assert.DoesNotContain(
            viewModel.EmployeeOptions,
            option =>
                option.EmployeeId ==
                linkedEmployeeId);

        Assert.Null(
            viewModel
                .SelectedEmployeeOption
                ?.EmployeeId);
    }

    [Fact]
    public async Task
        CreateAccountAsync_ForwardsPasswordWithoutStoringIt()
    {
        var creationService =
            new TestCreationService();

        var viewModel =
            new CreateStandardAccountViewModel(
                creationService,
                new TestQueryService(
                    EmptySnapshot()));

        await viewModel.LoadAsync();

        viewModel.Username =
            "hr.manager";

        viewModel.DisplayName =
            "Quản lý nhân sự";

        bool created =
            false;

        viewModel.AccountCreated +=
            (_, _) =>
                created =
                    true;

        await viewModel
            .CreateAccountCommand
            .ExecuteAsync(
                new CreateStandardAccountPasswords(
                    "Temporary-Test-Password-2026",
                    "Temporary-Test-Password-2026"));

        Assert.True(
            created);

        Assert.True(
            creationService.Called);

        Assert.NotNull(
            creationService.LastRequest);

        Assert.Equal(
            "hr.manager",
            creationService
                .LastRequest!
                .Username);

        Assert.Equal(
            "Quản lý nhân sự",
            creationService
                .LastRequest!
                .DisplayName);

        Assert.Equal(
            "Temporary-Test-Password-2026",
            creationService
                .LastRequest!
                .Password);

        Assert.Null(
            creationService
                .LastRequest!
                .EmployeeId);

        Assert.Null(
            typeof(CreateStandardAccountViewModel)
                .GetProperty(
                    "Password"));
    }

    [Fact]
    public async Task
        CreateAccountAsync_WithMismatchedConfirmation_DoesNotCallService()
    {
        var creationService =
            new TestCreationService();

        var viewModel =
            new CreateStandardAccountViewModel(
                creationService,
                new TestQueryService(
                    EmptySnapshot()));

        await viewModel.LoadAsync();

        viewModel.Username =
            "hr.manager";

        viewModel.DisplayName =
            "Quản lý nhân sự";

        await viewModel
            .CreateAccountCommand
            .ExecuteAsync(
                new CreateStandardAccountPasswords(
                    "Temporary-Test-Password-2026",
                    "Different-Password-2026"));

        Assert.False(
            creationService.Called);

        Assert.False(
            string.IsNullOrWhiteSpace(
                viewModel.ErrorMessage));
    }

    [Fact]
    public async Task
        LoadAsync_WhenQueryFails_FailsClosed()
    {
        var viewModel =
            new CreateStandardAccountViewModel(
                new TestCreationService(),
                new TestQueryService(
                    new InvalidOperationException(
                        "Database failure.")));

        await viewModel.LoadAsync();

        Assert.False(
            viewModel.IsReady);

        Assert.Empty(
            viewModel.EmployeeOptions);

        Assert.False(
            string.IsNullOrWhiteSpace(
                viewModel.ErrorMessage));
    }

    private static AccountManagementSnapshot
        EmptySnapshot()
    {
        return new AccountManagementSnapshot(
            Array.Empty<
                AccountManagementAccountItem>(),
            Array.Empty<
                AccountManagementRoleItem>(),
            Array.Empty<
                AccountManagementEmployeeItem>());
    }

    private sealed class TestCreationService
        : IStandardAccountCreationService
    {
        public bool Called
        {
            get;
            private set;
        }

        public CreateStandardAccountRequest?
            LastRequest
        {
            get;
            private set;
        }

        public CreateStandardAccountResult Result
        {
            get;
            set;
        } =
            new(
                true,
                Guid.NewGuid());

        public Task<CreateStandardAccountResult>
            CreateAsync(
                CreateStandardAccountRequest request,
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
}
