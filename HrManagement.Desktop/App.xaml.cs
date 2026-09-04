using System.Windows;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;
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
using HrManagement.Desktop.Diagnostics;
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
using Microsoft.Extensions.Logging;

namespace HrManagement.Desktop;

public partial class App : System.Windows.Application
{
    private ServiceProvider? _serviceProvider;

    private ILogger<App>? _logger;

    private ICrashDiagnosticService?
        _crashDiagnosticService;

    private IDiagnosticEnvelopeFactory?
        _diagnosticEnvelopeFactory;

    private IDiagnosticOutbox?
        _diagnosticOutbox;

    private int _fatalExceptionHandling;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var services = new ServiceCollection();

        ConfigureServices(services);

        _serviceProvider = services.BuildServiceProvider();

        _logger =
        _serviceProvider.GetRequiredService<
        ILogger<App>>();

        _logger.LogInformation(
            DiagnosticEventIds.ApplicationStarted,
            "Application started.");

        _crashDiagnosticService =
            _serviceProvider.GetRequiredService<
                ICrashDiagnosticService>();

        _diagnosticEnvelopeFactory =
            _serviceProvider.GetRequiredService<
                IDiagnosticEnvelopeFactory>();

        _diagnosticOutbox =
            _serviceProvider.GetRequiredService<
                IDiagnosticOutbox>();

        DispatcherUnhandledException +=
            OnDispatcherUnhandledException;

        AppDomain.CurrentDomain.UnhandledException +=
            OnAppDomainUnhandledException;

        TaskScheduler.UnobservedTaskException +=
            OnUnobservedTaskException;

        IApplicationThemeService applicationThemeService =
            _serviceProvider.GetRequiredService<
                IApplicationThemeService>();

        await applicationThemeService.InitializeAsync();

        DatabaseInitializer databaseInitializer =
            _serviceProvider.GetRequiredService<DatabaseInitializer>();

        try
        {
            await databaseInitializer.InitializeAsync();

            _logger.LogInformation(
                DiagnosticEventIds.DatabaseInitialized,
                "Database initialized.");
        }
        catch (Exception ex)
        {
            _logger.LogError(
                DiagnosticEventIds.DatabaseInitializationFailed,
                ex,
                "Database initialization failed.");

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

        _logger.LogInformation(
            DiagnosticEventIds.LoginWindowOpened,
            "Login window opened.");

        bool? loginResult = loginWindow.ShowDialog();

        if (loginResult != true)
        {
            _logger.LogInformation(
                DiagnosticEventIds.LoginCancelled,
                "Login was not completed.");

            Shutdown();
            return;
        }

        var mainWindow =
            _serviceProvider.GetRequiredService<MainWindow>();

        MainWindow = mainWindow;

        ShutdownMode = ShutdownMode.OnMainWindowClose;

        mainWindow.Show();

        _logger.LogInformation(
            DiagnosticEventIds.MainWindowOpened,
            "Main window opened.");
    }

    private static void ConfigureServices(
    IServiceCollection services)
    {
        DiagnosticLogOptions diagnosticLogOptions =
            DiagnosticLogOptions.CreateDefault();

        services.AddSingleton(
            diagnosticLogOptions);

        services.AddLogging(
            builder =>
            {
                builder.ClearProviders();

                builder.SetMinimumLevel(
                    LogLevel.Information);

                builder.AddProvider(
                    new SafeFileLoggerProvider(
                        diagnosticLogOptions));
            });

        CrashDiagnosticOptions crashDiagnosticOptions =
            CrashDiagnosticOptions.CreateDefault();

        services.AddSingleton(
            crashDiagnosticOptions);

        services.AddSingleton<
            ICrashDiagnosticService,
            CrashDiagnosticService>();

        services.AddSingleton<
            IDiagnosticEnvelopeFactory,
            DiagnosticEnvelopeFactory>();

        DiagnosticOutboxOptions diagnosticOutboxOptions =
            DiagnosticOutboxOptions.CreateDefault();

        services.AddSingleton(
            diagnosticOutboxOptions);

        services.AddSingleton<
            IDiagnosticOutbox,
            DiagnosticOutbox>();

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

        services.AddSingleton<
            IApplicationThemePreferenceStore,
            JsonApplicationThemePreferenceStore>();

        services.AddSingleton<
            ISystemAppearanceSource,
            WindowsSystemAppearanceSource>();

        services.AddSingleton<
            IApplicationThemeService,
            ApplicationThemeService>();

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

        services.AddTransient<SettingsViewModel>();

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

    private CrashDiagnosticResult?
    CaptureAndQueueDiagnostic(
        Exception exception,
        CrashOrigin origin,
        bool processTerminating)
    {
        CrashDiagnosticResult? report =
            _crashDiagnosticService?.TryCapture(
                exception,
                origin,
                processTerminating);

        if (report is null)
        {
            return null;
        }

        try
        {
            if (_diagnosticEnvelopeFactory is null
                || _diagnosticOutbox is null)
            {
                return report;
            }

            DiagnosticEnvelope envelope =
                _diagnosticEnvelopeFactory.Create(
                    report.Document);

            DiagnosticOutboxItem? queuedItem =
                _diagnosticOutbox.TryEnqueue(
                    envelope);

            _logger?.Log(
                queuedItem is null
                    ? LogLevel.Warning
                    : LogLevel.Information,
                queuedItem is null
                    ? DiagnosticEventIds.DiagnosticQueueFailed
                    : DiagnosticEventIds.DiagnosticQueued,
                "Diagnostic queue operation completed.");
        }
        catch
        {
            /*
             * Diagnostics must never cause a second
             * failure while handling the first one.
             */
            _logger?.LogWarning(
                DiagnosticEventIds.DiagnosticQueueFailed,
                "Diagnostic queue operation failed.");
        }

        return report;
    }

    private void OnDispatcherUnhandledException(
    object sender,
    DispatcherUnhandledExceptionEventArgs e)
    {
        if (Interlocked.Exchange(
                ref _fatalExceptionHandling,
                1)
            != 0)
        {
            return;
        }

        CrashDiagnosticResult? report =
            CaptureAndQueueDiagnostic(
                e.Exception,
                CrashOrigin.DispatcherUnhandledException,
                processTerminating:
                    false);

        _logger?.LogCritical(
            DiagnosticEventIds.DispatcherUnhandledException,
            e.Exception,
            "Unhandled dispatcher exception.");

        /*
         * Do not let WPF continue execution in an
         * unknown state. We handle the exception only
         * so that we can show the crash id and perform
         * an orderly shutdown.
         */
        e.Handled =
            true;

        TryShowFatalError(
            report?.CrashId);

        Shutdown(
            -1);
    }

    private void OnAppDomainUnhandledException(
        object? sender,
        UnhandledExceptionEventArgs e)
    {
        if (e.ExceptionObject
            is not Exception exception)
        {
            return;
        }

        CaptureAndQueueDiagnostic(
            exception,
            CrashOrigin.AppDomainUnhandledException,
            processTerminating:
                e.IsTerminating);

        _logger?.LogCritical(
            DiagnosticEventIds.AppDomainUnhandledException,
            exception,
            "Unhandled AppDomain exception.");

        /*
         * The runtime may already be terminating here,
         * so displaying UI is intentionally avoided.
         */
    }

    private void OnUnobservedTaskException(
        object? sender,
        UnobservedTaskExceptionEventArgs e)
    {
        CaptureAndQueueDiagnostic(
            e.Exception,
            CrashOrigin.UnobservedTaskException,
            processTerminating:
                false);

        _logger?.LogError(
            DiagnosticEventIds.UnobservedTaskException,
            e.Exception,
            "Unobserved task exception.");

        /*
         * This event does not mean the whole application
         * is corrupted. Record it and mark it observed.
         */
        e.SetObserved();
    }

    private static void TryShowFatalError(
        string? crashId)
    {
        try
        {
            string crashReference =
                string.IsNullOrWhiteSpace(
                    crashId)
                    ? "Không thể tạo mã sự cố."
                    : $"Mã sự cố: {crashId}";

            MessageBox.Show(
                "Ứng dụng gặp lỗi nghiêm trọng "
                + "và cần đóng.\n\n"
                + crashReference
                + "\n\nVui lòng cung cấp mã này "
                + "cho bộ phận hỗ trợ.",
                "HR Management - Lỗi nghiêm trọng",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
        catch
        {
            // Never throw from the fatal-error UI.
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        DispatcherUnhandledException -=
            OnDispatcherUnhandledException;

        AppDomain.CurrentDomain.UnhandledException -=
            OnAppDomainUnhandledException;

        TaskScheduler.UnobservedTaskException -=
            OnUnobservedTaskException;

        _logger?.LogInformation(
            DiagnosticEventIds.ApplicationExited,
            "Application exited.");

        _serviceProvider?.Dispose();

        base.OnExit(e);
    }
}
