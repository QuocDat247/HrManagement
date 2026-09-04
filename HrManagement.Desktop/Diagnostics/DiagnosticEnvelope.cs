namespace HrManagement.Desktop.Diagnostics;

public sealed record DiagnosticEnvelope(
    int SchemaVersion,
    string DiagnosticId,
    string Kind,
    DateTime CreatedAtUtc,
    DiagnosticApplicationMetadata Application,
    DiagnosticCrashPayload Crash);

public sealed record DiagnosticApplicationMetadata(
    string Version,
    string OperatingSystem,
    string Framework);

public sealed record DiagnosticCrashPayload(
    string CrashId,
    DateTime TimestampUtc,
    string Origin,
    bool ProcessTerminating,
    string ExceptionType,
    int HResult,
    string? StackTrace,
    IReadOnlyList<string> InnerExceptionTypes);
