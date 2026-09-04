namespace HrManagement.Desktop.Diagnostics;

public sealed record CrashDiagnosticDocument(
    int SchemaVersion,
    string CrashId,
    DateTime TimestampUtc,
    string Origin,
    bool ProcessTerminating,
    string ApplicationVersion,
    string OperatingSystem,
    string Framework,
    SafeExceptionDiagnostic Exception);

public sealed record SafeExceptionDiagnostic(
    string Type,
    int HResult,
    string? StackTrace,
    IReadOnlyList<string> InnerExceptionTypes);
