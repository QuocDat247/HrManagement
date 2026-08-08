namespace HrManagement.Desktop.Navigation;

public interface INavigationService
{
    object? CurrentViewModel { get; }

    event EventHandler? CurrentViewModelChanged;

    void NavigateTo<TViewModel>()
        where TViewModel : class;
}