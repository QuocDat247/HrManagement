namespace HrManagement.Desktop.Diagnostics;

public interface IDiagnosticEnvelopeFactory
{
    DiagnosticEnvelope Create(
        CrashDiagnosticDocument crashDocument);
}
