namespace HrManagement.Desktop.Diagnostics;

public sealed class DiagnosticEnvelopeFactory :
    IDiagnosticEnvelopeFactory
{
    public const int CurrentSchemaVersion = 1;

    private readonly TimeProvider _timeProvider;

    public DiagnosticEnvelopeFactory(
        TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(
            timeProvider);

        _timeProvider =
            timeProvider;
    }

    public DiagnosticEnvelope Create(
        CrashDiagnosticDocument crashDocument)
    {
        ArgumentNullException.ThrowIfNull(
            crashDocument);

        if (crashDocument.SchemaVersion != 1)
        {
            throw new NotSupportedException(
                $"Crash diagnostic schema "
                + $"{crashDocument.SchemaVersion} "
                + "is not supported.");
        }

        DateTime createdAtUtc =
            _timeProvider
                .GetUtcNow()
                .UtcDateTime;

        string diagnosticId =
            CreateDiagnosticId(
                createdAtUtc);

        return new DiagnosticEnvelope(
            SchemaVersion:
                CurrentSchemaVersion,
            DiagnosticId:
                diagnosticId,
            Kind:
                "Crash",
            CreatedAtUtc:
                createdAtUtc,
            Application:
                new DiagnosticApplicationMetadata(
                    Version:
                        crashDocument.ApplicationVersion,
                    OperatingSystem:
                        crashDocument.OperatingSystem,
                    Framework:
                        crashDocument.Framework),
            Crash:
                new DiagnosticCrashPayload(
                    CrashId:
                        crashDocument.CrashId,
                    TimestampUtc:
                        crashDocument.TimestampUtc,
                    Origin:
                        crashDocument.Origin,
                    ProcessTerminating:
                        crashDocument.ProcessTerminating,
                    ExceptionType:
                        crashDocument.Exception.Type,
                    HResult:
                        crashDocument.Exception.HResult,
                    StackTrace:
                        crashDocument.Exception.StackTrace,
                    InnerExceptionTypes:
                        crashDocument
                            .Exception
                            .InnerExceptionTypes
                            .ToArray()));
    }

    private static string CreateDiagnosticId(
        DateTime utcNow)
    {
        string suffix =
            Guid.NewGuid()
                .ToString("N")[..8]
                .ToUpperInvariant();

        return
            $"DIAG-{utcNow:yyyyMMdd}-{suffix}";
    }
}
