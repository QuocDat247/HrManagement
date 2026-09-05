namespace HrManagement.Desktop.Diagnostics;

public enum DiagnosticSendOutcome
{
    Sent = 1,
    Disabled = 2,
    RetryableFailure = 3,

    /*
     * The envelope reached the server but the server
     * permanently rejected that payload.
     */
    Rejected = 4,

    /*
     * Transport/client configuration is invalid.
     * The envelope itself must remain pending.
     */
    ConfigurationFailure = 5
}

public sealed record DiagnosticSendResult(
    DiagnosticSendOutcome Outcome,
    int? HttpStatusCode = null)
{
    public bool IsSuccess =>
        Outcome == DiagnosticSendOutcome.Sent;
}

public interface IDiagnosticReportSender
{
    Task<DiagnosticSendResult> SendAsync(
        DiagnosticEnvelope envelope,
        CancellationToken cancellationToken =
            default);
}
