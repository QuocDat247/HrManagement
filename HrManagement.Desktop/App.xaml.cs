using System.Windows;
using HrManagement.Application.Authentication;
using HrManagement.Application.Dashboard;
using HrManagement.Desktop.Navigation;
using HrManagement.Desktop.Theming;
using HrManagement.Desktop.ViewModels;
using HrManagement.Desktop.Views;
using HrManagement.Infrastructure.Authentication;
using HrManagement.Infrastructure.Dashboard;
using HrManagement.Infrastructure.DependencyInjection;
using HrManagement.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;

namespace HrManagement.Desktop;

public partial class App : System.Windows.Application
{
    private ServiceProvider? _serviceProvider;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var services = new ServiceCollection();

        ConfigureServices(services);

        _serviceProvider = services.BuildServiceProvider();

        DatabaseInitializer databaseInitializer =
            _serviceProvider.GetRequiredService<DatabaseInitializer>();

        try
        {
            await databaseInitializer.InitializeAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Không thể khởi tạo cơ sở dữ liệu.\n\n{ex.Message}",
                "Lỗi cơ sở dữ liệu",
                MessageBoxButton.OK,
                MessageBoxImage.Error);

            Shutdown();
            return;
        }

        ShutdownMode = ShutdownMode.OnExplicitShutdown;

        var loginWindow =
            _serviceProvider.GetRequiredService<LoginWindow>();

        bool? loginResult = loginWindow.ShowDialog();

        if (loginResult != true)
        {
            Shutdown();
            return;
        }

        var mainWindow =
            _serviceProvider.GetRequiredService<MainWindow>();

        MainWindow = mainWindow;

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
        services.AddInfrastructure();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _serviceProvider?.Dispose();

        base.OnExit(e);
    }
}
