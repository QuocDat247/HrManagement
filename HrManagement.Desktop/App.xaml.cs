using System.Windows;
using HrManagement.Application.Authentication;
using HrManagement.Application.Dashboard;
using HrManagement.Application.Employees.EmploymentHistories;
using HrManagement.Application.Employees.OrganizationAssignments;
using HrManagement.Application.Employees.Profiles;
using HrManagement.Application.Employees.Profiles.Completion;
using HrManagement.Application.Organization.Assignments;
using HrManagement.Application.Organization.Departments;
using HrManagement.Application.Organization.Positions;
using HrManagement.Desktop.Navigation;
using HrManagement.Desktop.Services;
using HrManagement.Desktop.Services.Departments;
using HrManagement.Desktop.Services.Positions;
using HrManagement.Desktop.Theming;
using HrManagement.Desktop.ViewModels;
using HrManagement.Desktop.Views;
using HrManagement.Infrastructure.Authentication;
using HrManagement.Infrastructure.Dashboard;
using HrManagement.Infrastructure.DependencyInjection;
using HrManagement.Infrastructure.Employees;
using HrManagement.Infrastructure.Employees.Profiles;
using HrManagement.Infrastructure.Organization.Assignments;
using HrManagement.Infrastructure.Organization.Departments;
using HrManagement.Infrastructure.Organization.Positions;
using HrManagement.Infrastructure.Persistence;
using HrManagement.Application.Auditing;
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
            CurrentUserSession>();

        services.AddSingleton<
            IUserSession>(
                provider =>
                    provider.GetRequiredService<
                        CurrentUserSession>());

        services.AddSingleton<
            ICurrentUserContext>(
                provider =>
                    provider.GetRequiredService<
                        CurrentUserSession>());

        services.AddSingleton<TimeProvider>(
            TimeProvider.System);

        services.AddSingleton<
            IAuditEntryFactory,
            AuditEntryFactory>();

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

        services.AddTransient<AddEmployeeViewModel>();

        services.AddTransient<AddEmployeeWindow>();

        services.AddSingleton<IEmployeeDialogService, EmployeeDialogService>();

        services.AddTransient<EditEmployeeViewModel>();

        services.AddTransient<EditEmployeeWindow>();

        services.AddTransient<AttendanceLeaveWorkspaceViewModel>();

        services.AddTransient<WorkScheduleWorkspaceViewModel>();

        services.AddTransient<HolidayExceptionWorkspaceViewModel>();

        services.AddTransient<MonthlyTimesheetWorkspaceViewModel>();

        services.AddTransient<OvertimeWorkspaceViewModel>();

        services.AddTransient<PayrollWorkspaceViewModel>();

        services.AddSingleton<
            IEmployeeNavigationService,
            EmployeeNavigationService>();

        services.AddTransient<DeactivateEmployeeViewModel>();

        services.AddTransient<DeactivateEmployeeWindow>();

        services.AddTransient<
            CancelEmployeeDeactivationViewModel>();

        services.AddTransient<
            CancelEmployeeDeactivationWindow>();

        services.AddTransient<
            RehireEmployeeViewModel>();

        services.AddTransient<
            RehireEmployeeWindow>();

        services.AddSingleton<
            IEmploymentHistoryService,
            EmploymentHistoryService>();

        services.AddTransient<
            EmployeeEmploymentHistoryViewModel>();

        services.AddTransient<
            EmployeeEmploymentHistoryWindow>();

        services.AddSingleton<
            IDepartmentRepository,
            EfDepartmentRepository>();

        services.AddSingleton<
            IDepartmentService,
            DepartmentService>();

        services.AddTransient<
            DepartmentEditorViewModel>();

        services.AddTransient<
            DepartmentEditorWindow>();

        services.AddSingleton<
            IDepartmentDialogService,
            DepartmentDialogService>();

        services.AddTransient<DepartmentsViewModel>();

        services.AddSingleton<
            IPositionRepository,
            EfPositionRepository>();

        services.AddSingleton<
            IPositionService,
            PositionService>();

        services.AddTransient<
            PositionEditorViewModel>();

        services.AddTransient<
            PositionEditorWindow>();

        services.AddSingleton<
            IPositionDialogService,
            PositionDialogService>();

        services.AddTransient<PositionsViewModel>();

        services.AddSingleton<
            IEmployeeOrganizationBackfillService,
            EfEmployeeOrganizationBackfillService>();

        services.AddTransient<
            OrganizationEmployeesViewModel>();

        services.AddTransient<
            OrganizationEmployeesWindow>();

        services.AddSingleton<
            IEmployeeOrganizationTransferPersistence,
            EfEmployeeOrganizationTransferPersistence>();

        services.AddTransient<
            TransferEmployeeViewModel>();

        services.AddTransient<
            TransferEmployeeWindow>();

        services.AddSingleton<
            IEmployeeOrganizationHistoryService,
            EmployeeOrganizationHistoryService>();

        services.AddTransient<
            EmployeeOrganizationHistoryViewModel>();

        services.AddTransient<
            EmployeeOrganizationHistoryWindow>();

        services.AddTransient<
            EmployeePersonalProfileSectionViewModel>();

        services.AddTransient<
            EmployeeProfileViewModel>();

        services.AddTransient<
            EmployeeProfileWindow>();

        services.AddTransient<
            EmployeeAddressSectionViewModel>();

        services.AddSingleton<
            IEmployeeEmergencyContactRepository,
            EfEmployeeEmergencyContactRepository>();

        services.AddTransient<
            EmployeeEmergencyContactSectionViewModel>();

        services.AddTransient<
            EmployeeIdentificationRecordSectionViewModel>();

        services.AddSingleton<
            IConfirmationDialogService,
            ConfirmationDialogService>();

        services.AddSingleton<
            IEmployeeProfileCompletionPolicy,
            EmployeeProfileCompletionPolicy>();

        services.AddTransient<
            IEmployeeProfileCompletionService,
            EmployeeProfileCompletionService>();

        services.AddSingleton<
            IUserConfirmationService,
            WpfUserConfirmationService>();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _serviceProvider?.Dispose();

        base.OnExit(e);
    }
}
