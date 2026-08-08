using Microsoft.Extensions.DependencyInjection;

namespace HrManagement.Desktop.Navigation;

public sealed class NavigationService : INavigationService
{
    private readonly IServiceProvider _serviceProvider;

    private object? _currentViewModel;

    public NavigationService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public object? CurrentViewModel => _currentViewModel;

    public event EventHandler? CurrentViewModelChanged;

    public void NavigateTo<TViewModel>()
        where TViewModel : class
    {
        TViewModel viewModel =
            _serviceProvider.GetRequiredService<TViewModel>();

        _currentViewModel = viewModel;

        CurrentViewModelChanged?.Invoke(
            this,
            EventArgs.Empty);
    }
}