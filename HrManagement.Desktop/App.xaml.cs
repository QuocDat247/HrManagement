using HrManagement.Application.Authentication;
using HrManagement.Desktop.ViewModels;
using HrManagement.Desktop.Views;
using HrManagement.Infrastructure.Authentication;
using Microsoft.Extensions.DependencyInjection;
using HrManagement.Desktop.Navigation;
using HrManagement.Desktop.Theming;
using System.Windows;

namespace HrManagement.Desktop;

public partial class App : System.Windows.Application
{
    private ServiceProvider? _serviceProvider;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        ShutdownMode = ShutdownMode.OnExplicitShutdown;

        ServiceCollection services = new();

        ConfigureServices(services);

        _serviceProvider = services.BuildServiceProvider();

        LoginWindow loginWindow =
            _serviceProvider.GetRequiredService<LoginWindow>();

        bool? loginResult = loginWindow.ShowDialog();

        if (loginResult != true)
        {
            Shutdown();
            return;
        }

        MainWindow mainWindow =
            _serviceProvider.GetRequiredService<MainWindow>();

        this.MainWindow = mainWindow;

        ShutdownMode = ShutdownMode.OnMainWindowClose;

        mainWindow.Show();
    }

    private static void ConfigureServices(
    IServiceCollection services)
    {
        services.AddSingleton<
            IAuthenticationService,
            FakeAuthenticationService>();

        services.AddSingleton<
            INavigationService,
            NavigationService>();

        services.AddTransient<LoginViewModel>();
        services.AddTransient<MainViewModel>();

        services.AddTransient<LoginWindow>();
        services.AddTransient<MainWindow>();
        services.AddTransient<DashboardViewModel>();
        services.AddTransient<EmployeesViewModel>();
        services.AddSingleton<IThemeService, ThemeService>();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _serviceProvider?.Dispose();

        base.OnExit(e);
    }
}