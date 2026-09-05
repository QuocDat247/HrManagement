namespace HrManagement.Desktop.Diagnostics;

public sealed record DiagnosticProcessingResult(
    int ExaminedCount,
    int SentCount,
    int RejectedCount,
    bool Deferred);

public interface IDiagnosticOutboxProcessor
{
    Task<DiagnosticProcessingResult>
        ProcessPendingAsync(
            CancellationToken cancellationToken =
                default);
}
