using System;
using HrManagement.Application.Persistence.Backups;
using HrManagement.Desktop.BuildIdentity;
using HrManagement.Desktop.Services;
using HrManagement.Desktop.Services.DatabaseMaintenance;
using HrManagement.Desktop.Diagnostics;
using HrManagement.Desktop.Theming;
using HrManagement.Desktop.ViewModels;

namespace HrManagement.Tests.Theming;

public sealed class SettingsViewModelTests
{
    [Fact]
    public void Constructor_LoadsCurrentThemePreference()
    {
        var themeService =
            new StubApplicationThemeService
            {
                CurrentPreferenceValue =
                    new ApplicationThemePreference(
                        ApplicationAppearance.Dark,
                        ApplicationAccent.Green),

                EffectiveAppearanceValue =
                    ApplicationAppearance.Dark
            };

        var diagnosticConsentService =
            new StubDiagnosticConsentService();

        var viewModel =
            CreateViewModel(
                themeService,
                diagnosticConsentService);

        Assert.Equal(
            ApplicationAppearance.Dark,
            viewModel.SelectedAppearance);

        Assert.Equal(
            ApplicationAccent.Green,
            viewModel.SelectedAccent);

        Assert.Equal(
            3,
            viewModel.AppearanceOptions.Count);

        Assert.Equal(
            2,
            viewModel.AccentOptions.Count);

        Assert.False(
            viewModel.HasChanges);

        Assert.False(
            viewModel.CanApplyChanges);
    }

    [Fact]
    public void Constructor_ExposesBuildIdentity()
    {
        var viewModel =
            CreateViewModel(
                new StubApplicationThemeService(),
                new StubDiagnosticConsentService());

        Assert.Equal(
            "1.0.0-test",
            viewModel.BuildCoreVersion);

        Assert.Equal(
            "Standard",
            viewModel.BuildEdition);

        Assert.Equal(
            "TestCustomer",
            viewModel.BuildCustomer);

        Assert.Equal(
            "Testing",
            viewModel.BuildReleaseChannel);
    }

    [Fact]
    public async Task ApplyAsync_WhenSelectionChanges_AppliesAndPersistsPreference()
    {
        var themeService =
            new StubApplicationThemeService
            {
                CurrentPreferenceValue =
                    new ApplicationThemePreference(
                        ApplicationAppearance.Light,
                        ApplicationAccent.Blue),

                EffectiveAppearanceValue =
                    ApplicationAppearance.Light
            };

        var diagnosticConsentService =
            new StubDiagnosticConsentService();

        var viewModel =
            CreateViewModel(
                themeService,
                diagnosticConsentService);

        viewModel.SelectedAppearance =
            ApplicationAppearance.Dark;

        viewModel.SelectedAccent =
            ApplicationAccent.Green;

        Assert.True(
            viewModel.HasChanges);

        await viewModel.ApplyCommand.ExecuteAsync(
            null);

        Assert.Equal(
            1,
            themeService.ApplyCallCount);

        Assert.Equal(
            ApplicationAppearance.Dark,
            themeService.CurrentPreference.Appearance);

        Assert.Equal(
            ApplicationAccent.Green,
            themeService.CurrentPreference.Accent);

        Assert.False(
            viewModel.HasChanges);

        Assert.False(
            viewModel.CanApplyChanges);

        Assert.Equal(
            "Đã áp dụng và lưu cài đặt.",
            viewModel.SuccessMessage);
    }

    [Fact]
    public void LoadCommand_RestoresServicePreference()
    {
        var themeService =
            new StubApplicationThemeService
            {
                CurrentPreferenceValue =
                    new ApplicationThemePreference(
                        ApplicationAppearance.Light,
                        ApplicationAccent.Blue),

                EffectiveAppearanceValue =
                    ApplicationAppearance.Light
            };

        var diagnosticConsentService =
            new StubDiagnosticConsentService();

        var viewModel =
            CreateViewModel(
                themeService,
                diagnosticConsentService);

        viewModel.SelectedAppearance =
            ApplicationAppearance.Dark;

        Assert.True(
            viewModel.HasChanges);

        viewModel.LoadCommand.Execute(
            null);

        Assert.Equal(
            ApplicationAppearance.Light,
            viewModel.SelectedAppearance);

        Assert.Equal(
            ApplicationAccent.Blue,
            viewModel.SelectedAccent);

        Assert.False(
            viewModel.HasChanges);
    }

    [Fact]
    public async Task ApplyAsync_WhenDiagnosticConsentChanges_AppliesConsent()
    {
        var themeService =
            new StubApplicationThemeService
            {
                CurrentPreferenceValue =
                    new ApplicationThemePreference(
                        ApplicationAppearance.Light,
                        ApplicationAccent.Blue),

                EffectiveAppearanceValue =
                    ApplicationAppearance.Light
            };

        var diagnosticConsentService =
            new StubDiagnosticConsentService();

        var viewModel =
            CreateViewModel(
                themeService,
                diagnosticConsentService);

        Assert.False(
            viewModel.SelectedAllowDiagnosticUpload);

        viewModel.SelectedAllowDiagnosticUpload =
            true;

        Assert.True(
            viewModel.HasChanges);

        await viewModel.ApplyCommand.ExecuteAsync(
            null);

        Assert.Equal(
            0,
            themeService.ApplyCallCount);

        Assert.Equal(
            1,
            diagnosticConsentService.ApplyCallCount);

        Assert.True(
            diagnosticConsentService
                .CurrentPreference
                .AllowDiagnosticUpload);

        Assert.False(
            viewModel.HasChanges);

        Assert.Equal(
            "Đã áp dụng và lưu cài đặt.",
            viewModel.SuccessMessage);
    }

    private static SettingsViewModel CreateViewModel(
    IApplicationThemeService themeService,
    IDiagnosticConsentService diagnosticConsentService)
    {
        return new SettingsViewModel(
            themeService,
            diagnosticConsentService,
            new TestServiceProvider(),
            new StubOwnerDatabaseMaintenanceService(),
            new StubDatabaseBackupFileDialogService(),
            new StubConfirmationDialogService(),
            new StubApplicationExitService(),
            new ApplicationBuildIdentity(
                CoreVersion:
                    "1.0.0-test",
                Edition:
                    "Standard",
                CustomerCode:
                    "TestCustomer",
                ReleaseChannel:
                    "Testing"));
    }

    private sealed class StubDiagnosticConsentService
    : IDiagnosticConsentService
    {
        public DiagnosticConsentPreference
            CurrentPreferenceValue
        {
            get;
            set;
        } =
            DiagnosticConsentPreference.Default;

        public DiagnosticConsentPreference
            CurrentPreference =>
                CurrentPreferenceValue;

        public int ApplyCallCount
        {
            get;
            private set;
        }

        public Task InitializeAsync(
            CancellationToken cancellationToken =
                default)
        {
            return Task.CompletedTask;
        }

        public Task ApplyAsync(
            DiagnosticConsentPreference preference,
            CancellationToken cancellationToken =
                default)
        {
            ApplyCallCount++;

            CurrentPreferenceValue =
                preference;

            return Task.CompletedTask;
        }
    }

    private sealed class StubApplicationThemeService
        : IApplicationThemeService
    {
        public ApplicationThemePreference
            CurrentPreferenceValue
        {
            get;
            set;
        } =
            ApplicationThemePreference.Default;

        public ApplicationAppearance
            EffectiveAppearanceValue
        {
            get;
            set;
        } =
            ApplicationAppearance.Light;

        public int ApplyCallCount
        {
            get;
            private set;
        }

        public ApplicationThemePreference
            CurrentPreference =>
                CurrentPreferenceValue;

        public ApplicationAppearance
            EffectiveAppearance =>
                EffectiveAppearanceValue;

        public Task InitializeAsync(
            CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public Task ApplyAsync(
            ApplicationThemePreference preference,
            CancellationToken cancellationToken = default)
        {
            ApplyCallCount++;

            CurrentPreferenceValue =
                preference;

            EffectiveAppearanceValue =
                preference.Appearance ==
                    ApplicationAppearance.System
                    ? ApplicationAppearance.Light
                    : preference.Appearance;

            return Task.CompletedTask;
        }
    }

    private sealed class StubOwnerDatabaseMaintenanceService
    : IOwnerDatabaseMaintenanceService
    {
        public Task<bool> CanManageAsync(
            CancellationToken cancellationToken = default)
        {
            cancellationToken
                .ThrowIfCancellationRequested();

            return Task.FromResult(
                false);
        }

        public Task<DatabaseBackupResult>
            CreateBackupAsync(
                string destinationFilePath,
                CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task<DatabaseRestoreResult>
            RestoreAsync(
                string backupFilePath,
                CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }
    }

    private sealed class StubDatabaseBackupFileDialogService
        : IDatabaseBackupFileDialogService
    {
        public string? SelectBackupDestination()
        {
            return null;
        }

        public string? SelectBackupForRestore()
        {
            return null;
        }
    }

    private sealed class StubConfirmationDialogService
        : IConfirmationDialogService
    {
        public bool Confirm(
            string title,
            string message)
        {
            return false;
        }
    }

    private sealed class StubApplicationExitService
        : IApplicationExitService
    {
        public void Shutdown()
        {
        }
    }

    private sealed class TestServiceProvider
    : IServiceProvider
    {
        public object? GetService(
            Type serviceType)
        {
            return null;
        }
    }
}
