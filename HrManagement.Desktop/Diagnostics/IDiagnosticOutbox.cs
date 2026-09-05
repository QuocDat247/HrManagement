namespace HrManagement.Desktop.Diagnostics;

public sealed record DiagnosticOutboxItem(
    string DiagnosticId,
    string FilePath);

public interface IDiagnosticOutbox
{
    DiagnosticOutboxItem? TryEnqueue(
        DiagnosticEnvelope envelope);

    IReadOnlyList<DiagnosticOutboxItem>
        GetPendingItems();

    DiagnosticEnvelope? TryRead(
        DiagnosticOutboxItem item);

    bool TryDelete(
        DiagnosticOutboxItem item);

    bool TryQuarantine(
        DiagnosticOutboxItem item);
}
