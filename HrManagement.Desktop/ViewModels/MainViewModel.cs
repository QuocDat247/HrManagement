using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HrManagement.Desktop.Navigation;
using HrManagement.Desktop.Theming;

namespace HrManagement.Desktop.ViewModels;

public sealed partial class MainViewModel : ObservableObject
{
    private readonly INavigationService _navigationService;
    private readonly IThemeService _themeService;

    [ObservableProperty]
    private NavigationItem? _selectedNavigationItem;
    private void ToggleTheme()
    {
        AppTheme nextTheme =
            _themeService.CurrentTheme == AppTheme.Blue
                ? AppTheme.Green
                : AppTheme.Blue;

        _themeService.ApplyTheme(nextTheme);
    }

    // Constructor>
    public MainViewModel(
        INavigationService navigationService,
        IThemeService themeService)
    {
        _navigationService = navigationService;
        _themeService = themeService;

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
                "Chấm công & Nghỉ phép",
                typeof(AttendanceLeaveWorkspaceViewModel))
        ];

        NavigateCommand =
            new RelayCommand<NavigationItem>(Navigate);

        SelectedNavigationItem = NavigationItems[0];

        _navigationService.NavigateTo(
            SelectedNavigationItem.ViewModelType);

        ToggleThemeCommand =
        new RelayCommand(ToggleTheme);
    }
    // <

    // Command>
    public IRelayCommand ToggleThemeCommand { get; }

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

