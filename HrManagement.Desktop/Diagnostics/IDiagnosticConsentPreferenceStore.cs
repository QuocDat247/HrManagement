namespace HrManagement.Desktop.Diagnostics;

public interface IDiagnosticConsentPreferenceStore
{
    Task<DiagnosticConsentPreference?> LoadAsync(
        CancellationToken cancellationToken =
            default);

    Task SaveAsync(
        DiagnosticConsentPreference preference,
        CancellationToken cancellationToken =
            default);
}
