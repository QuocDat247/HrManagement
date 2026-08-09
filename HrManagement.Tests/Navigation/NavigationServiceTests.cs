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

    [Fact]
    public void NavigateTo_WhenReturningToPreviousViewModel_ReusesCachedInstance()
    {
        var services = new ServiceCollection();

        services.AddTransient<FirstViewModel>();
        services.AddTransient<SecondViewModel>();

        using ServiceProvider provider = services.BuildServiceProvider();

        var navigationService = new NavigationService(provider);

        navigationService.NavigateTo<FirstViewModel>();

        object? firstInstance =
            navigationService.CurrentViewModel;

        navigationService.NavigateTo<SecondViewModel>();
        navigationService.NavigateTo<FirstViewModel>();

        object? returnedInstance =
            navigationService.CurrentViewModel;

        Assert.Same(firstInstance, returnedInstance);
    }

    private sealed class TestViewModel
    {
    }

    private sealed class FirstViewModel
    {
    }

    private sealed class SecondViewModel
    {
    }
}
