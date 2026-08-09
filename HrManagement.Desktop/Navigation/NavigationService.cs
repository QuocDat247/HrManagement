using Microsoft.Extensions.DependencyInjection;

namespace HrManagement.Desktop.Navigation;

public sealed class NavigationService : INavigationService
{
    private readonly IServiceProvider _serviceProvider;
    private object? _currentViewModel;

    public object? CurrentViewModel => _currentViewModel;

    public event EventHandler? CurrentViewModelChanged;

    public NavigationService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public void NavigateTo<TViewModel>()
        where TViewModel : class
    {
        NavigateTo(typeof(TViewModel));
    }

    public void NavigateTo(Type viewModelType)
    {
        ArgumentNullException.ThrowIfNull(viewModelType);

        if (_currentViewModel?.GetType() == viewModelType)
        {
            return;
        }

        _currentViewModel =
            _serviceProvider.GetRequiredService(viewModelType);

        CurrentViewModelChanged?.Invoke(this, EventArgs.Empty);
    }
}
