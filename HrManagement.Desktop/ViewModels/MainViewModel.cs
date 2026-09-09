using HrManagement.Application.Authorization;
using HrManagement.Domain.Authorization.Permissions;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HrManagement.Desktop.Navigation;
using HrManagement.Application.Authentication;

namespace HrManagement.Desktop.ViewModels;

public sealed partial class MainViewModel : ObservableObject
{
    private readonly INavigationService _navigationService;

    private readonly IAuthorizationService
        _authorizationService;

    private bool _isInitialized;

    [ObservableProperty]
    private NavigationItem? _selectedNavigationItem;

    // Constructor>
    public MainViewModel(
    INavigationService navigationService,
    ICurrentUserContext currentUserContext,
    IAuthorizationService authorizationService)
    {
        _navigationService =
            navigationService;

        _authorizationService =
            authorizationService;

        CurrentUserDisplayName =
            currentUserContext.CurrentUser?.DisplayName
            ?? currentUserContext.CurrentUser?.Username
            ?? "Người dùng";

        _navigationService.CurrentViewModelChanged +=
            OnCurrentViewModelChanged;

        NavigationItems =
            Array.Empty<NavigationItem>();

        NavigateCommand =
            new RelayCommand<NavigationItem>(
                Navigate);
    }

    // Command>
    public string CurrentUserDisplayName
    {
        get;
    }

    public IReadOnlyList<NavigationItem> NavigationItems
    {
        get;
        private set;
    }

    public object? CurrentViewModel =>
        _navigationService.CurrentViewModel;

    public IRelayCommand<NavigationItem> NavigateCommand { get; }
    // <

    public async Task InitializeAsync(
        CancellationToken cancellationToken = default)
    {
        if (_isInitialized)
        {
            return;
        }

        var navigationItems =
            new List<NavigationItem>
            {
                new(
                    "Tổng quan",
                    typeof(DashboardViewModel))
            };

        if (await _authorizationService
                .HasPermissionAsync(
                    PermissionCodes.EmployeeView,
                    cancellationToken))
        {
            navigationItems.Add(
                new NavigationItem(
                    "Nhân viên",
                    typeof(EmployeesViewModel)));
        }

        if (await _authorizationService
                .HasPermissionAsync(
                    PermissionCodes.DepartmentView,
                    cancellationToken))
        {
            navigationItems.Add(
                new NavigationItem(
                    "Phòng ban",
                    typeof(DepartmentsViewModel)));
        }

        if (await _authorizationService
                .HasPermissionAsync(
                    PermissionCodes.PositionView,
                    cancellationToken))
        {
            navigationItems.Add(
                new NavigationItem(
                    "Chức danh",
                    typeof(PositionsViewModel)));
        }

        if (await _authorizationService
                .HasPermissionAsync(
                    PermissionCodes.WorkScheduleView,
                    cancellationToken))
        {
            navigationItems.Add(
                new NavigationItem(
                    "Lịch làm việc",
                    typeof(WorkScheduleWorkspaceViewModel)));
        }

        if (await _authorizationService
                .HasPermissionAsync(
                    PermissionCodes.HolidayExceptionView,
                    cancellationToken))
        {
            navigationItems.Add(
                new NavigationItem(
                    "Ngày lễ & Ngoại lệ",
                    typeof(HolidayExceptionWorkspaceViewModel)));
        }

        if (await _authorizationService
                .HasPermissionAsync(
                    PermissionCodes.TimesheetView,
                    cancellationToken))
        {
            navigationItems.Add(
                new NavigationItem(
                    "Bảng công tháng",
                    typeof(MonthlyTimesheetWorkspaceViewModel)));
        }

        if (await _authorizationService
                .HasPermissionAsync(
                    PermissionCodes.OvertimeView,
                    cancellationToken))
        {
            navigationItems.Add(
                new NavigationItem(
                    "Tăng ca",
                    typeof(OvertimeWorkspaceViewModel)));
        }

        if (await _authorizationService
                .HasPermissionAsync(
                    PermissionCodes.PayrollView,
                    cancellationToken))
        {
            navigationItems.Add(
                new NavigationItem(
                    "Bảng lương",
                    typeof(PayrollWorkspaceViewModel)));
        }

        bool canViewAttendance =
            await _authorizationService
                .HasPermissionAsync(
                    PermissionCodes.AttendanceView,
                    cancellationToken);

        bool canViewLeave =
            canViewAttendance
            && await _authorizationService
                .HasPermissionAsync(
                    PermissionCodes.LeaveView,
                    cancellationToken);

        if (canViewAttendance
            && canViewLeave)
        {
            navigationItems.Add(
                new NavigationItem(
                    "Chấm công & Nghỉ phép",
                    typeof(AttendanceLeaveWorkspaceViewModel)));
        }

        navigationItems.AddRange(
            new[]
            {
                new NavigationItem(
                    "Cài đặt",
                    typeof(SettingsViewModel))
            });

        NavigationItems =
            navigationItems;

        OnPropertyChanged(
            nameof(NavigationItems));

        SelectedNavigationItem =
            NavigationItems[0];

        _navigationService.NavigateTo(
            SelectedNavigationItem.ViewModelType);

        _isInitialized =
            true;
    }

    private void Navigate(NavigationItem? item)
    {
        if (item is null)
        {
            return;
        }

        SelectedNavigationItem = item;

        _navigationService.NavigateTo(item.ViewModelType);
    }

    private void OnCurrentViewModelChanged(
        object? sender,
        EventArgs e)
    {
        OnPropertyChanged(nameof(CurrentViewModel));
    }
}

