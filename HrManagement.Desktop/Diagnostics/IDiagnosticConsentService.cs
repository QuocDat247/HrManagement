namespace HrManagement.Desktop.Diagnostics;

public interface IDiagnosticConsentService
{
    DiagnosticConsentPreference CurrentPreference
    {
        get;
    }

    Task InitializeAsync(
        CancellationToken cancellationToken =
            default);

    Task ApplyAsync(
        DiagnosticConsentPreference preference,
        CancellationToken cancellationToken =
            default);
}
