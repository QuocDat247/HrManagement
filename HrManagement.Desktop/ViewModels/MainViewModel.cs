using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HrManagement.Desktop.Navigation;

namespace HrManagement.Desktop.ViewModels;

public sealed partial class MainViewModel : ObservableObject
{
    private readonly INavigationService _navigationService;

    [ObservableProperty]
    private NavigationItem? _selectedNavigationItem;

    public MainViewModel(INavigationService navigationService)
    {
        _navigationService = navigationService;

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
                typeof(EmployeesViewModel))
        ];

        NavigateCommand =
            new RelayCommand<NavigationItem>(Navigate);

        SelectedNavigationItem = NavigationItems[0];

        _navigationService.NavigateTo(
            SelectedNavigationItem.ViewModelType);
    }

    public IReadOnlyList<NavigationItem> NavigationItems { get; }

    public object? CurrentViewModel =>
        _navigationService.CurrentViewModel;

    public IRelayCommand<NavigationItem> NavigateCommand { get; }

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

