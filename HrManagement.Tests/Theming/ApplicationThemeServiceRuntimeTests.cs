using HrManagement.Desktop.Theming;

namespace HrManagement.Tests.Theming;

public sealed class ApplicationThemeServiceRuntimeTests
{
    [Fact]
    public async Task ApplyAsync_DarkGreen_ChangesCurrentPreferenceAndEffectiveAppearance()
    {
        System.Windows.Application? application =
            System.Windows.Application.Current;

        bool createdApplication =
            false;

        if (application is null)
        {
            application =
                new System.Windows.Application();

            createdApplication =
                true;
        }

        try
        {
            var store =
                new StubPreferenceStore();

            var systemAppearanceSource =
                new StubSystemAppearanceSource(
                    ApplicationAppearance.Light);

            var service =
                new ApplicationThemeService(
                    store,
                    systemAppearanceSource);

            var preference =
                new ApplicationThemePreference(
                    ApplicationAppearance.Dark,
                    ApplicationAccent.Green);

            await service.ApplyAsync(
                preference);

            Assert.Equal(
                preference,
                service.CurrentPreference);

            Assert.Equal(
                ApplicationAppearance.Dark,
                service.EffectiveAppearance);

            Assert.Equal(
                preference,
                store.SavedPreference);
        }
        finally
        {
            if (createdApplication)
            {
                application.Shutdown();
            }
        }
    }

    [Fact]
    public async Task InitializeAsync_SystemPreference_UsesCurrentSystemAppearance()
    {
        System.Windows.Application? application =
            System.Windows.Application.Current;

        bool createdApplication =
            false;

        if (application is null)
        {
            application =
                new System.Windows.Application();

            createdApplication =
                true;
        }

        try
        {
            var preference =
                new ApplicationThemePreference(
                    ApplicationAppearance.System,
                    ApplicationAccent.Blue);

            var store =
                new StubPreferenceStore
                {
                    LoadedPreference =
                        preference
                };

            var systemAppearanceSource =
                new StubSystemAppearanceSource(
                    ApplicationAppearance.Dark);

            var service =
                new ApplicationThemeService(
                    store,
                    systemAppearanceSource);

            await service.InitializeAsync();

            Assert.Equal(
                preference,
                service.CurrentPreference);

            Assert.Equal(
                ApplicationAppearance.Dark,
                service.EffectiveAppearance);
        }
        finally
        {
            if (createdApplication)
            {
                application.Shutdown();
            }
        }
    }

    private sealed class StubPreferenceStore
        : IApplicationThemePreferenceStore
    {
        public ApplicationThemePreference? LoadedPreference
        {
            get;
            set;
        }

        public ApplicationThemePreference? SavedPreference
        {
            get;
            private set;
        }

        public Task<ApplicationThemePreference?> LoadAsync(
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(
                LoadedPreference);
        }

        public Task SaveAsync(
            ApplicationThemePreference preference,
            CancellationToken cancellationToken = default)
        {
            SavedPreference =
                preference;

            return Task.CompletedTask;
        }
    }

    private sealed class StubSystemAppearanceSource
        : ISystemAppearanceSource
    {
        private readonly ApplicationAppearance
            _appearance;

        public StubSystemAppearanceSource(
            ApplicationAppearance appearance)
        {
            _appearance =
                appearance;
        }

        public ApplicationAppearance GetCurrentAppearance()
        {
            return _appearance;
        }
    }
}
