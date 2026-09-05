namespace HrManagement.Desktop.Diagnostics;

public sealed record DiagnosticConsentPreference(
    bool AllowDiagnosticUpload)
{
    public static DiagnosticConsentPreference Default =>
        new(
            AllowDiagnosticUpload:
                false);
}
