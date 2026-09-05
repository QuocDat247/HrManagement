namespace HrManagement.Desktop.Diagnostics;

public sealed class DiagnosticConsentService :
    IDiagnosticConsentService
{
    private readonly IDiagnosticConsentPreferenceStore
        _preferenceStore;

    public DiagnosticConsentService(
        IDiagnosticConsentPreferenceStore preferenceStore)
    {
        ArgumentNullException.ThrowIfNull(
            preferenceStore);

        _preferenceStore =
            preferenceStore;
    }

    public DiagnosticConsentPreference CurrentPreference
    {
        get;
        private set;
    } =
        DiagnosticConsentPreference.Default;

    public async Task InitializeAsync(
        CancellationToken cancellationToken =
            default)
    {
        DiagnosticConsentPreference preference =
            await _preferenceStore.LoadAsync(
                cancellationToken)
            ?? DiagnosticConsentPreference.Default;

        CurrentPreference =
            preference;
    }

    public async Task ApplyAsync(
        DiagnosticConsentPreference preference,
        CancellationToken cancellationToken =
            default)
    {
        ArgumentNullException.ThrowIfNull(
            preference);

        await _preferenceStore.SaveAsync(
            preference,
            cancellationToken);

        CurrentPreference =
            preference;
    }
}
