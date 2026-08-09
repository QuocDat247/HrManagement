using HrManagement.Desktop.Navigation;
using Microsoft.Extensions.DependencyInjection;

namespace HrManagement.Tests.Navigation;

public sealed class NavigationServiceTests
{
    [Fact]
    public void NavigateTo_WhenNavigatingToSameViewModel_DoesNotCreateNewInstance()
    {
        var services = new ServiceCollection();

        services.AddTransient<TestViewModel>();

        using ServiceProvider provider = services.BuildServiceProvider();

        var navigationService = new NavigationService(provider);

        navigationService.NavigateTo<TestViewModel>();

        object? firstInstance = navigationService.CurrentViewModel;

        navigationService.NavigateTo<TestViewModel>();

        object? secondInstance = navigationService.CurrentViewModel;

        Assert.Same(firstInstance, secondInstance);
    }

    private sealed class TestViewModel
    {
    }
}
