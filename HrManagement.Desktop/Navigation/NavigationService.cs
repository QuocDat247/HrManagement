using Microsoft.Extensions.DependencyInjection;

namespace HrManagement.Desktop.Navigation;

public sealed class NavigationService : INavigationService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly Dictionary<Type, object> _viewModelCache = new();

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

        if (!_viewModelCache.TryGetValue(
                viewModelType,
                out object? viewModel))
        {
            viewModel =
                _serviceProvider.GetRequiredService(viewModelType);

            _viewModelCache.Add(viewModelType, viewModel);
        }

        _currentViewModel = viewModel;

        CurrentViewModelChanged?.Invoke(
            this,
            EventArgs.Empty);
    }
}
