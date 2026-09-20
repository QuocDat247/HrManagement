using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using HrManagement.Application.Persistence.Backups;
using HrManagement.Desktop.Services;
using HrManagement.Desktop.Services.DatabaseMaintenance;
using HrManagement.Desktop.Views;
using HrManagement.Desktop.Theming;
using HrManagement.Desktop.Diagnostics;

namespace HrManagement.Desktop.ViewModels;

public sealed partial class SettingsViewModel
    : ObservableObject
{
    private readonly IApplicationThemeService
        _themeService;

    private readonly IDiagnosticConsentService
        _diagnosticConsentService;

    private readonly IServiceProvider
        _serviceProvider;

    private readonly IOwnerDatabaseMaintenanceService
        _databaseMaintenanceService;

    private readonly IDatabaseBackupFileDialogService
        _databaseBackupFileDialogService;

    private readonly IConfirmationDialogService
        _confirmationDialogService;

    private readonly IApplicationExitService
        _applicationExitService;

    [ObservableProperty]
    private ApplicationAppearance selectedAppearance;

    [ObservableProperty]
    private ApplicationAccent selectedAccent;

    [ObservableProperty]
    private bool selectedAllowDiagnosticUpload;

    [ObservableProperty]
    private bool isApplying;

    [ObservableProperty]
    private string? errorMessage;

    [ObservableProperty]
    private string? successMessage;

    [ObservableProperty]
    private bool hasDatabaseMaintenanceAccess;

    [ObservableProperty]
    private bool isDatabaseMaintenanceBusy;

    [ObservableProperty]
    private string? databaseMaintenanceMessage;

    [ObservableProperty]
    private string? databaseMaintenanceError;

    public SettingsViewModel(
        IApplicationThemeService themeService,
        IDiagnosticConsentService diagnosticConsentService,
        IServiceProvider serviceProvider,
        IOwnerDatabaseMaintenanceService databaseMaintenanceService,
        IDatabaseBackupFileDialogService databaseBackupFileDialogService,
        IConfirmationDialogService confirmationDialogService,
        IApplicationExitService applicationExitService)
    {
        _themeService =
            themeService;

        _diagnosticConsentService =
            diagnosticConsentService;

        _serviceProvider =
            serviceProvider;

        _databaseMaintenanceService =
            databaseMaintenanceService;

        _databaseBackupFileDialogService =
            databaseBackupFileDialogService;

        _confirmationDialogService =
            confirmationDialogService;

        _applicationExitService =
            applicationExitService;

        AppearanceOptions =
        [
            new SettingsAppearanceOption(
                ApplicationAppearance.System,
                "Theo hệ thống",
                "Tự động dùng giao diện sáng hoặc tối theo Windows."),

            new SettingsAppearanceOption(
                ApplicationAppearance.Light,
                "Sáng",
                "Sử dụng giao diện sáng cho toàn bộ ứng dụng."),

            new SettingsAppearanceOption(
                ApplicationAppearance.Dark,
                "Tối",
                "Sử dụng giao diện tối cho toàn bộ ứng dụng.")
        ];

        AccentOptions =
        [
            new SettingsAccentOption(
                ApplicationAccent.Blue,
                "Xanh dương",
                "Màu chủ đạo cân bằng, phù hợp giao diện quản trị."),

            new SettingsAccentOption(
                ApplicationAccent.Green,
                "Xanh lá",
                "Màu chủ đạo xanh lá cho navigation và hành động chính.")
        ];

        ChangePasswordCommand =
            new RelayCommand(
                ChangePassword);

        LoadCommand =
            new RelayCommand(
                Load);

        ApplyCommand =
            new AsyncRelayCommand(
                ApplyAsync,
                CanApply);

        LoadDatabaseMaintenanceAccessCommand =
            new AsyncRelayCommand(
                LoadDatabaseMaintenanceAccessAsync);

        CreateDatabaseBackupCommand =
            new AsyncRelayCommand(
                CreateDatabaseBackupAsync,
                CanManageDatabase);

        RestoreDatabaseCommand =
            new AsyncRelayCommand(
                RestoreDatabaseAsync,
                CanManageDatabase);

        Load();
    }

    public IReadOnlyList<SettingsAppearanceOption>
        AppearanceOptions
    {
        get;
    }

    public IReadOnlyList<SettingsAccentOption>
        AccentOptions
    {
        get;
    }

    public IRelayCommand ChangePasswordCommand
    {
        get;
    }

    public IRelayCommand LoadCommand
    {
        get;
    }

    public IAsyncRelayCommand ApplyCommand
    {
        get;
    }

    public IAsyncRelayCommand
        LoadDatabaseMaintenanceAccessCommand
    {
        get;
    }

    public IAsyncRelayCommand
        CreateDatabaseBackupCommand
    {
        get;
    }

    public IAsyncRelayCommand
        RestoreDatabaseCommand
    {
        get;
    }

    public bool HasChanges =>
        SelectedAppearance !=
            _themeService.CurrentPreference.Appearance
        || SelectedAccent !=
            _themeService.CurrentPreference.Accent
        || SelectedAllowDiagnosticUpload !=
            _diagnosticConsentService
                .CurrentPreference
                .AllowDiagnosticUpload;

    public bool CanApplyChanges =>
        CanApply();

    public string CurrentThemeText
    {
        get
        {
            string appearance =
                _themeService.CurrentPreference.Appearance switch
                {
                    ApplicationAppearance.System =>
                        "Theo hệ thống",

                    ApplicationAppearance.Light =>
                        "Sáng",

                    ApplicationAppearance.Dark =>
                        "Tối",

                    _ =>
                        "Không xác định"
                };

            string accent =
                _themeService.CurrentPreference.Accent switch
                {
                    ApplicationAccent.Blue =>
                        "Xanh dương",

                    ApplicationAccent.Green =>
                        "Xanh lá",

                    _ =>
                        "Không xác định"
                };

            return
                $"{appearance} • {accent}";
        }
    }

    public string EffectiveAppearanceText =>
        _themeService.EffectiveAppearance switch
        {
            ApplicationAppearance.Dark =>
                "Hiện đang hiển thị giao diện tối.",

            _ =>
                "Hiện đang hiển thị giao diện sáng."
        };

    partial void OnSelectedAppearanceChanged(
        ApplicationAppearance value)
    {
        NotifySelectionState();
    }

    partial void OnSelectedAccentChanged(
        ApplicationAccent value)
    {
        NotifySelectionState();
    }

    partial void OnSelectedAllowDiagnosticUploadChanged(
        bool value)
    {
        NotifySelectionState();
    }

    partial void OnHasDatabaseMaintenanceAccessChanged(
    bool value)
    {
        NotifyDatabaseMaintenanceCommandState();
    }

    partial void OnIsDatabaseMaintenanceBusyChanged(
        bool value)
    {
        NotifyDatabaseMaintenanceCommandState();
    }

    private void ChangePassword()
    {
        ChangePasswordWindow window =
            _serviceProvider.GetRequiredService<
                ChangePasswordWindow>();

        window.Owner =
            System.Windows.Application.Current.MainWindow;

        bool? result =
            window.ShowDialog();

        if (result == true)
        {
            SuccessMessage =
                "Đã đổi mật khẩu thành công.";

            ErrorMessage =
                null;
        }
    }

    private void Load()
    {
        ApplicationThemePreference preference =
            _themeService.CurrentPreference;

        SelectedAppearance =
            preference.Appearance;

        SelectedAccent =
            preference.Accent;

        SelectedAllowDiagnosticUpload =
            _diagnosticConsentService
                .CurrentPreference
                .AllowDiagnosticUpload;

        ErrorMessage =
            null;

        SuccessMessage =
            null;

        NotifySelectionState();
    }

    private async Task ApplyAsync()
    {
        if (!CanApply())
        {
            return;
        }

        ErrorMessage =
            null;

        SuccessMessage =
            null;

        IsApplying =
            true;

        NotifySelectionState();

        try
        {
            var themePreference =
                new ApplicationThemePreference(
                    SelectedAppearance,
                    SelectedAccent);

            var diagnosticPreference =
                new DiagnosticConsentPreference(
                    SelectedAllowDiagnosticUpload);

            bool themeChanged =
                themePreference !=
                    _themeService.CurrentPreference;

            bool diagnosticConsentChanged =
                diagnosticPreference !=
                    _diagnosticConsentService
                        .CurrentPreference;

            if (themeChanged)
            {
                await _themeService.ApplyAsync(
                    themePreference);
            }

            if (diagnosticConsentChanged)
            {
                await _diagnosticConsentService
                    .ApplyAsync(
                        diagnosticPreference);
            }

            SuccessMessage =
                "Đã áp dụng và lưu cài đặt.";

            OnPropertyChanged(
                nameof(CurrentThemeText));

            OnPropertyChanged(
                nameof(EffectiveAppearanceText));
        }
        catch (ArgumentException exception)
        {
            ErrorMessage =
                CleanArgumentMessage(
                    exception);
        }
        catch (InvalidOperationException exception)
        {
            ErrorMessage =
                exception.Message;
        }
        catch (IOException exception)
        {
            ErrorMessage =
                $"Không thể lưu cài đặt ứng dụng: {exception.Message}";
        }
        catch (UnauthorizedAccessException)
        {
            ErrorMessage =
                "Ứng dụng không có quyền lưu cài đặt.";
        }
        finally
        {
            IsApplying =
                false;

            NotifySelectionState();
        }
    }

    private async Task
    LoadDatabaseMaintenanceAccessAsync()
    {
        try
        {
            HasDatabaseMaintenanceAccess =
                await _databaseMaintenanceService
                    .CanManageAsync();
        }
        catch
        {
            HasDatabaseMaintenanceAccess =
                false;
        }
    }

    private async Task
        CreateDatabaseBackupAsync()
    {
        if (!CanManageDatabase())
        {
            return;
        }

        string? destinationFilePath =
            _databaseBackupFileDialogService
                .SelectBackupDestination();

        if (string.IsNullOrWhiteSpace(
                destinationFilePath))
        {
            return;
        }

        DatabaseMaintenanceError =
            null;

        DatabaseMaintenanceMessage =
            null;

        IsDatabaseMaintenanceBusy =
            true;

        try
        {
            DatabaseBackupResult result =
                await _databaseMaintenanceService
                    .CreateBackupAsync(
                        destinationFilePath);

            DatabaseMaintenanceMessage =
                "Đã tạo bản sao lưu thành công:\n"
                + result.BackupFilePath;
        }
        catch (UnauthorizedAccessException exception)
        {
            DatabaseMaintenanceError =
                exception.Message;

            HasDatabaseMaintenanceAccess =
                false;
        }
        catch (IOException exception)
        {
            DatabaseMaintenanceError =
                "Không thể ghi file backup: "
                + exception.Message;
        }
        catch (InvalidOperationException exception)
        {
            DatabaseMaintenanceError =
                exception.Message;
        }
        finally
        {
            IsDatabaseMaintenanceBusy =
                false;
        }
    }

    private async Task
        RestoreDatabaseAsync()
    {
        if (!CanManageDatabase())
        {
            return;
        }

        string? backupFilePath =
            _databaseBackupFileDialogService
                .SelectBackupForRestore();

        if (string.IsNullOrWhiteSpace(
                backupFilePath))
        {
            return;
        }

        bool firstConfirmation =
            _confirmationDialogService
                .Confirm(
                    "Khôi phục dữ liệu",
                    "Khôi phục sẽ thay thế toàn bộ dữ liệu hiện tại "
                    + "bằng dữ liệu trong file backup đã chọn.\n\n"
                    + "Ứng dụng sẽ tự tạo một safety backup "
                    + "của database hiện tại trước khi thay thế.\n\n"
                    + "Bạn có muốn tiếp tục không?");

        if (!firstConfirmation)
        {
            return;
        }

        bool finalConfirmation =
            _confirmationDialogService
                .Confirm(
                    "Xác nhận khôi phục lần cuối",
                    "Sau khi khôi phục thành công, ứng dụng sẽ đóng ngay "
                    + "để tránh tiếp tục sử dụng dữ liệu cũ đang được giữ "
                    + "trong bộ nhớ.\n\n"
                    + "Hãy đảm bảo mọi thao tác đang làm đã hoàn tất.\n\n"
                    + "Tiếp tục khôi phục?");

        if (!finalConfirmation)
        {
            return;
        }

        DatabaseMaintenanceError =
            null;

        DatabaseMaintenanceMessage =
            null;

        IsDatabaseMaintenanceBusy =
            true;

        try
        {
            DatabaseRestoreResult result =
                await _databaseMaintenanceService
                    .RestoreAsync(
                        backupFilePath);

            if (!result.IsSuccessful)
            {
                DatabaseMaintenanceError =
                    result.ErrorMessage
                    ?? "Không thể khôi phục database.";

                if (!string.IsNullOrWhiteSpace(
                        result.SafetyBackupFilePath))
                {
                    DatabaseMaintenanceError +=
                        "\n\nSafety backup:\n"
                        + result.SafetyBackupFilePath;
                }

                return;
            }

            string safetyBackupText =
                string.IsNullOrWhiteSpace(
                    result.SafetyBackupFilePath)
                    ? "Không có thông tin đường dẫn safety backup."
                    : result.SafetyBackupFilePath;

            MessageBox.Show(
                "Khôi phục dữ liệu thành công.\n\n"
                + "Safety backup của database trước khi khôi phục:\n"
                + safetyBackupText
                + "\n\nỨng dụng sẽ đóng ngay. "
                + "Vui lòng mở lại HR Management.",
                "Khôi phục hoàn tất",
                MessageBoxButton.OK,
                MessageBoxImage.Information);

            _applicationExitService
                .Shutdown();
        }
        catch (UnauthorizedAccessException exception)
        {
            DatabaseMaintenanceError =
                exception.Message;

            HasDatabaseMaintenanceAccess =
                false;
        }
        catch (InvalidOperationException exception)
        {
            DatabaseMaintenanceError =
                exception.Message;
        }
        catch (IOException exception)
        {
            DatabaseMaintenanceError =
                "Không thể đọc file backup: "
                + exception.Message;
        }
        finally
        {
            IsDatabaseMaintenanceBusy =
                false;
        }
    }

    private bool CanManageDatabase()
    {
        return HasDatabaseMaintenanceAccess
            && !IsDatabaseMaintenanceBusy;
    }

    private void
        NotifyDatabaseMaintenanceCommandState()
    {
        CreateDatabaseBackupCommand?
            .NotifyCanExecuteChanged();

        RestoreDatabaseCommand?
            .NotifyCanExecuteChanged();
    }

    private bool CanApply()
    {
        return !IsApplying
            && HasChanges;
    }

    private void NotifySelectionState()
    {
        OnPropertyChanged(
            nameof(HasChanges));

        OnPropertyChanged(
            nameof(CanApplyChanges));

        ApplyCommand?
            .NotifyCanExecuteChanged();
    }

    private static string CleanArgumentMessage(
        ArgumentException exception)
    {
        if (!string.IsNullOrWhiteSpace(
                exception.ParamName))
        {
            int markerIndex =
                exception.Message.IndexOf(
                    " (Parameter '",
                    StringComparison.Ordinal);

            if (markerIndex >= 0)
            {
                return exception.Message[
                    ..markerIndex];
            }
        }

        return exception.Message;
    }
}
