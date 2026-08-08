using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HrManagement.Desktop.Navigation;

namespace HrManagement.Desktop.ViewModels;

public sealed class MainViewModel : ObservableObject
{
    private readonly INavigationService _navigationService;

    public MainViewModel(INavigationService navigationService)
    {
        _navigationService = navigationService;

        _navigationService.CurrentViewModelChanged +=
            OnCurrentViewModelChanged;

        NavigateToDashboardCommand =
            new RelayCommand(NavigateToDashboard);

        NavigateToEmployeesCommand =
            new RelayCommand(NavigateToEmployees);

        _navigationService.NavigateTo<DashboardViewModel>();
    }

    public object? CurrentViewModel =>
        _navigationService.CurrentViewModel;

    public IRelayCommand NavigateToDashboardCommand { get; }

    public IRelayCommand NavigateToEmployeesCommand { get; }

    private void NavigateToDashboard()
    {
        _navigationService.NavigateTo<DashboardViewModel>();
    }

    private void NavigateToEmployees()
    {
        _navigationService.NavigateTo<EmployeesViewModel>();
    }

    private void OnCurrentViewModelChanged(
        object? sender,
        EventArgs e)
    {
        OnPropertyChanged(nameof(CurrentViewModel));
    }
}