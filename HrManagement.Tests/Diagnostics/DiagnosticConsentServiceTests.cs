using HrManagement.Desktop.Diagnostics;

namespace HrManagement.Tests.Diagnostics;

public sealed class DiagnosticConsentServiceTests
{
    [Fact]
    public async Task InitializeAsync_WhenNoStoredPreference_UsesDefaultOff()
    {
        var store =
            new StubStore(
                loadedPreference:
                    null);

        var service =
            new DiagnosticConsentService(
                store);

        await service.InitializeAsync();

        Assert.False(
            service
                .CurrentPreference
                .AllowDiagnosticUpload);
    }

    [Fact]
    public async Task InitializeAsync_LoadsStoredPreference()
    {
        var store =
            new StubStore(
                new DiagnosticConsentPreference(
                    AllowDiagnosticUpload:
                        true));

        var service =
            new DiagnosticConsentService(
                store);

        await service.InitializeAsync();

        Assert.True(
            service
                .CurrentPreference
                .AllowDiagnosticUpload);
    }

    [Fact]
    public async Task ApplyAsync_SavesAndUpdatesCurrentPreference()
    {
        var store =
            new StubStore(
                loadedPreference:
                    null);

        var service =
            new DiagnosticConsentService(
                store);

        var preference =
            new DiagnosticConsentPreference(
                AllowDiagnosticUpload:
                    true);

        await service.ApplyAsync(
            preference);

        Assert.Equal(
            preference,
            store.SavedPreference);

        Assert.Equal(
            preference,
            service.CurrentPreference);
    }

    private sealed class StubStore :
        IDiagnosticConsentPreferenceStore
    {
        private readonly DiagnosticConsentPreference?
            _loadedPreference;

        public StubStore(
            DiagnosticConsentPreference?
                loadedPreference)
        {
            _loadedPreference =
                loadedPreference;
        }

        public DiagnosticConsentPreference?
            SavedPreference
        {
            get;
            private set;
        }

        public Task<DiagnosticConsentPreference?>
            LoadAsync(
                CancellationToken cancellationToken =
                    default)
        {
            return Task.FromResult(
                _loadedPreference);
        }

        public Task SaveAsync(
            DiagnosticConsentPreference preference,
            CancellationToken cancellationToken =
                default)
        {
            SavedPreference =
                preference;

            return Task.CompletedTask;
        }
    }
}
