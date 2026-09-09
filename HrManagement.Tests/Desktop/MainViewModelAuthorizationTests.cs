using HrManagement.Application.Authentication;
using HrManagement.Application.Authorization;
using HrManagement.Desktop.Navigation;
using HrManagement.Desktop.ViewModels;
using HrManagement.Domain.Authorization.Permissions;

namespace HrManagement.Tests.Desktop;

public sealed class MainViewModelAuthorizationTests
{
    [Fact]
    public void
        Constructor_DoesNotNavigateBeforeInitialization()
    {
        var navigationService =
            new TestNavigationService();

        var viewModel =
            CreateViewModel(
                navigationService);

        Assert.Empty(
            viewModel.NavigationItems);

        Assert.Null(
            navigationService.LastNavigatedType);
    }

    [Fact]
    public async Task
        InitializeAsync_WithoutProtectedPermissions_HidesProtectedItems()
    {
        var navigationService =
            new TestNavigationService();

        var viewModel =
            CreateViewModel(
                navigationService);

        await viewModel.InitializeAsync();

        Assert.Contains(
            viewModel.NavigationItems,
            item =>
                item.ViewModelType ==
                typeof(DashboardViewModel));

        Assert.DoesNotContain(
            viewModel.NavigationItems,
            item =>
                item.ViewModelType ==
                typeof(EmployeesViewModel));

        Assert.DoesNotContain(
            viewModel.NavigationItems,
            item =>
                item.ViewModelType ==
                typeof(DepartmentsViewModel));

        Assert.DoesNotContain(
            viewModel.NavigationItems,
            item =>
                item.ViewModelType ==
                typeof(PositionsViewModel));

        Assert.DoesNotContain(
            viewModel.NavigationItems,
            item =>
                item.ViewModelType ==
                typeof(WorkScheduleWorkspaceViewModel));

        Assert.DoesNotContain(
            viewModel.NavigationItems,
            item =>
                item.ViewModelType ==
                typeof(HolidayExceptionWorkspaceViewModel));

        Assert.DoesNotContain(
            viewModel.NavigationItems,
            item =>
                item.ViewModelType ==
                typeof(MonthlyTimesheetWorkspaceViewModel));

        Assert.DoesNotContain(
            viewModel.NavigationItems,
            item =>
                item.ViewModelType ==
                typeof(OvertimeWorkspaceViewModel));

        Assert.DoesNotContain(
            viewModel.NavigationItems,
            item =>
                item.ViewModelType ==
                typeof(PayrollWorkspaceViewModel));

        Assert.Equal(
            typeof(DashboardViewModel),
            navigationService.LastNavigatedType);
    }

    [Fact]
    public async Task
        InitializeAsync_WithProtectedPermissions_ShowsProtectedItems()
    {
        var navigationService =
            new TestNavigationService();

        var viewModel =
            CreateViewModel(
                navigationService,
                PermissionCodes.EmployeeView,
                PermissionCodes.DepartmentView,
                PermissionCodes.PositionView,
                PermissionCodes.WorkScheduleView,
                PermissionCodes.HolidayExceptionView,
                PermissionCodes.TimesheetView,
                PermissionCodes.OvertimeView,
                PermissionCodes.PayrollView);

        await viewModel.InitializeAsync();

        Assert.Contains(
            viewModel.NavigationItems,
            item =>
                item.ViewModelType ==
                typeof(EmployeesViewModel));

        Assert.Contains(
            viewModel.NavigationItems,
            item =>
                item.ViewModelType ==
                typeof(DepartmentsViewModel));

        Assert.Contains(
            viewModel.NavigationItems,
            item =>
                item.ViewModelType ==
                typeof(PositionsViewModel));

        Assert.Contains(
            viewModel.NavigationItems,
            item =>
                item.ViewModelType ==
                typeof(WorkScheduleWorkspaceViewModel));

        Assert.Contains(
            viewModel.NavigationItems,
            item =>
                item.ViewModelType ==
                typeof(HolidayExceptionWorkspaceViewModel));

        Assert.Contains(
            viewModel.NavigationItems,
            item =>
                item.ViewModelType ==
                typeof(MonthlyTimesheetWorkspaceViewModel));

        Assert.Contains(
            viewModel.NavigationItems,
            item =>
                item.ViewModelType ==
                typeof(OvertimeWorkspaceViewModel));

        Assert.Contains(
            viewModel.NavigationItems,
            item =>
                item.ViewModelType ==
                typeof(PayrollWorkspaceViewModel));
    }

    [Fact]
    public async Task
        InitializeAsync_WithOnlyEmployeePermission_ShowsOnlyEmployeeProtectedItem()
    {
        var navigationService =
            new TestNavigationService();

        var viewModel =
            CreateViewModel(
                navigationService,
                PermissionCodes.EmployeeView);

        await viewModel.InitializeAsync();

        Assert.Contains(
            viewModel.NavigationItems,
            item =>
                item.ViewModelType ==
                typeof(EmployeesViewModel));

        Assert.DoesNotContain(
            viewModel.NavigationItems,
            item =>
                item.ViewModelType ==
                typeof(DepartmentsViewModel));

        Assert.DoesNotContain(
            viewModel.NavigationItems,
            item =>
                item.ViewModelType ==
                typeof(PositionsViewModel));

        Assert.DoesNotContain(
            viewModel.NavigationItems,
            item =>
                item.ViewModelType ==
                typeof(WorkScheduleWorkspaceViewModel));

        Assert.DoesNotContain(
            viewModel.NavigationItems,
            item =>
                item.ViewModelType ==
                typeof(HolidayExceptionWorkspaceViewModel));

        Assert.DoesNotContain(
            viewModel.NavigationItems,
            item =>
                item.ViewModelType ==
                typeof(MonthlyTimesheetWorkspaceViewModel));

        Assert.DoesNotContain(
            viewModel.NavigationItems,
            item =>
                item.ViewModelType ==
                typeof(OvertimeWorkspaceViewModel));

        Assert.DoesNotContain(
            viewModel.NavigationItems,
            item =>
                item.ViewModelType ==
                typeof(PayrollWorkspaceViewModel));
    }

    [Fact]
    public async Task
        InitializeAsync_WhenPermissionLookupFails_RemainsUninitialized()
    {
        var navigationService =
            new TestNavigationService();

        var authorizationService =
            new TestAuthorizationService
            {
                ThrowOnCheck =
                    true
            };

        var viewModel =
            new MainViewModel(
                navigationService,
                CreateCurrentUserContext(),
                authorizationService);

        await Assert.ThrowsAsync<
            InvalidOperationException>(
                () =>
                    viewModel.InitializeAsync());

        Assert.Empty(
            viewModel.NavigationItems);

        Assert.Null(
            navigationService.LastNavigatedType);
    }

    private static MainViewModel CreateViewModel(
        TestNavigationService navigationService,
        params string[] allowedPermissions)
    {
        return new MainViewModel(
            navigationService,
            CreateCurrentUserContext(),
            new TestAuthorizationService(
                allowedPermissions));
    }

    private static ICurrentUserContext
        CreateCurrentUserContext()
    {
        return new TestCurrentUserContext(
            new AuthenticatedUser(
                Guid.NewGuid()
                    .ToString("D"),
                "test-user",
                "Test User"));
    }

    private sealed class TestCurrentUserContext
        : ICurrentUserContext
    {
        public TestCurrentUserContext(
            AuthenticatedUser currentUser)
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

    private sealed class TestAuthorizationService
        : IAuthorizationService
    {
        private readonly HashSet<string>
            _allowedPermissions;

        public TestAuthorizationService(
            params string[] allowedPermissions)
        {
            _allowedPermissions =
                new HashSet<string>(
                    allowedPermissions,
                    StringComparer.Ordinal);
        }

        public bool ThrowOnCheck
        {
            get;
            init;
        }

        public Task<bool> HasPermissionAsync(
            string permissionCode,
            CancellationToken cancellationToken = default)
        {
            cancellationToken
                .ThrowIfCancellationRequested();

            if (ThrowOnCheck)
            {
                throw new InvalidOperationException(
                    "Permission lookup failed.");
            }

            return Task.FromResult(
                _allowedPermissions.Contains(
                    permissionCode));
        }
    }

    private sealed class TestNavigationService
        : INavigationService
    {
        public object? CurrentViewModel
        {
            get;
            private set;
        }

        public Type? LastNavigatedType
        {
            get;
            private set;
        }

        public event EventHandler?
            CurrentViewModelChanged;

        public void NavigateTo<TViewModel>()
            where TViewModel : class
        {
            NavigateTo(
                typeof(TViewModel));
        }

        public void NavigateTo(
            Type viewModelType)
        {
            LastNavigatedType =
                viewModelType;

            CurrentViewModel =
                new object();

            CurrentViewModelChanged?
                .Invoke(
                    this,
                    EventArgs.Empty);
        }
    }
}
