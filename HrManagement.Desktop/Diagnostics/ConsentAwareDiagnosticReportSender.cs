namespace HrManagement.Desktop.Diagnostics;

public sealed class ConsentAwareDiagnosticReportSender :
    IDiagnosticReportSender
{
    private readonly IDiagnosticConsentService
        _diagnosticConsentService;

    private readonly HttpDiagnosticReportSender
        _innerSender;

    public ConsentAwareDiagnosticReportSender(
        IDiagnosticConsentService diagnosticConsentService,
        HttpDiagnosticReportSender innerSender)
    {
        ArgumentNullException.ThrowIfNull(
            diagnosticConsentService);

        ArgumentNullException.ThrowIfNull(
            innerSender);

        _diagnosticConsentService =
            diagnosticConsentService;

        _innerSender =
            innerSender;
    }

    public Task<DiagnosticSendResult> SendAsync(
        DiagnosticEnvelope envelope,
        CancellationToken cancellationToken =
            default)
    {
        ArgumentNullException.ThrowIfNull(
            envelope);

        cancellationToken
            .ThrowIfCancellationRequested();

        if (!_diagnosticConsentService
                .CurrentPreference
                .AllowDiagnosticUpload)
        {
            return Task.FromResult(
                new DiagnosticSendResult(
                    DiagnosticSendOutcome
                        .NotAuthorized));
        }

        return _innerSender.SendAsync(
            envelope,
            cancellationToken);
    }
}
