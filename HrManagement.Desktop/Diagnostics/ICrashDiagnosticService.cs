namespace HrManagement.Desktop.Diagnostics;

public enum CrashOrigin
{
    DispatcherUnhandledException = 1,
    AppDomainUnhandledException = 2,
    UnobservedTaskException = 3
}

public sealed record CrashDiagnosticResult(
    string CrashId,
    string FilePath,
    CrashDiagnosticDocument Document);

public interface ICrashDiagnosticService
{
    CrashDiagnosticResult? TryCapture(
        Exception exception,
        CrashOrigin origin,
        bool processTerminating);
}
