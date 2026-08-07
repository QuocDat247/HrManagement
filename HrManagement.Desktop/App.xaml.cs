using HrManagement.Application.Authentication;
using HrManagement.Desktop.ViewModels;
using HrManagement.Infrastructure.Authentication;
using Microsoft.Extensions.DependencyInjection;
using HrManagement.Desktop.Views;
using System.Windows;

namespace HrManagement.Desktop;

public partial class App : System.Windows.Application
{
    private ServiceProvider? _serviceProvider;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        ServiceCollection services = new();

        ConfigureServices(services);

        _serviceProvider = services.BuildServiceProvider();

        LoginWindow loginWindow =
            _serviceProvider.GetRequiredService<LoginWindow>();

        loginWindow.Show();
    }

    private static void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton<
        IAuthenticationService,
        FakeAuthenticationService>();

        services.AddTransient<LoginViewModel>();
        services.AddTransient<LoginWindow>();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _serviceProvider?.Dispose();

        base.OnExit(e);
    }
}