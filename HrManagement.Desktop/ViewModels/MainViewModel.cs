using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HrManagement.Desktop.Navigation;
using HrManagement.Application.Authentication;

namespace HrManagement.Desktop.ViewModels;

public sealed partial class MainViewModel : ObservableObject
{
    private readonly INavigationService _navigationService;

    [ObservableProperty]
    private NavigationItem? _selectedNavigationItem;

    // Constructor>
    public MainViewModel(
        INavigationService navigationService,
        ICurrentUserContext currentUserContext)
    {
        _navigationService = navigationService;

        CurrentUserDisplayName =
            currentUserContext.CurrentUser?.DisplayName
            ?? currentUserContext.CurrentUser?.Username
            ?? "Người dùng";

        _navigationService.CurrentViewModelChanged +=
            OnCurrentViewModelChanged;

        // Sau này chỉ cần thêm:
        // new NavigationItem(
        // "Phòng ban",
        // typeof(DepartmentsViewModel))
        // chứ không cần viết thêm một command riêng.
        NavigationItems =
        [
            new NavigationItem(
                "Tổng quan",
                typeof(DashboardViewModel)),

            new NavigationItem(
                "Nhân viên",
                typeof(EmployeesViewModel)),

            new NavigationItem(
                "Phòng ban",
                typeof(DepartmentsViewModel)),

            new NavigationItem(
                "Chức danh",
                typeof(PositionsViewModel)),

            new NavigationItem(
                "Lịch làm việc",
                typeof(WorkScheduleWorkspaceViewModel)),

            new NavigationItem(
                "Ngày lễ & Ngoại lệ",
                typeof(HolidayExceptionWorkspaceViewModel)),

            new NavigationItem(
                "Bảng công tháng",
                typeof(MonthlyTimesheetWorkspaceViewModel)),

            new NavigationItem(
                "Tăng ca",
                typeof(OvertimeWorkspaceViewModel)),

            new NavigationItem(
                "Bảng lương",
                typeof(PayrollWorkspaceViewModel)),

            new NavigationItem(
                "Chấm công & Nghỉ phép",
                typeof(AttendanceLeaveWorkspaceViewModel)),

            new NavigationItem(
                "Cài đặt",
                typeof(SettingsViewModel))

        ];

        NavigateCommand =
            new RelayCommand<NavigationItem>(Navigate);

        SelectedNavigationItem = NavigationItems[0];

        _navigationService.NavigateTo(
            SelectedNavigationItem.ViewModelType);
    }

    // Command>
    public string CurrentUserDisplayName
    {
        get;
    }

    public IReadOnlyList<NavigationItem> NavigationItems { get; }

    public object? CurrentViewModel =>
        _navigationService.CurrentViewModel;

    public IRelayCommand<NavigationItem> NavigateCommand { get; }
    // <

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

