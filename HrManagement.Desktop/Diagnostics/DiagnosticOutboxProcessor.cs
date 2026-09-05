using Microsoft.Extensions.Logging;

namespace HrManagement.Desktop.Diagnostics;

public sealed class DiagnosticOutboxProcessor :
    IDiagnosticOutboxProcessor
{
    private readonly IDiagnosticOutbox _outbox;

    private readonly IDiagnosticReportSender _sender;

    private readonly DiagnosticOutboxProcessorOptions
        _options;

    private readonly ILogger<DiagnosticOutboxProcessor>
        _logger;

    public DiagnosticOutboxProcessor(
        IDiagnosticOutbox outbox,
        IDiagnosticReportSender sender,
        DiagnosticOutboxProcessorOptions options,
        ILogger<DiagnosticOutboxProcessor> logger)
    {
        ArgumentNullException.ThrowIfNull(
            outbox);

        ArgumentNullException.ThrowIfNull(
            sender);

        ArgumentNullException.ThrowIfNull(
            options);

        ArgumentNullException.ThrowIfNull(
            logger);

        if (options.MaxItemsPerRun < 1)
        {
            throw new ArgumentOutOfRangeException(
                nameof(options));
        }

        _outbox =
            outbox;

        _sender =
            sender;

        _options =
            options;

        _logger =
            logger;
    }

    public async Task<DiagnosticProcessingResult>
        ProcessPendingAsync(
            CancellationToken cancellationToken =
                default)
    {
        IReadOnlyList<DiagnosticOutboxItem>
            pendingItems =
                _outbox
                    .GetPendingItems()
                    .Take(
                        _options.MaxItemsPerRun)
                    .ToArray();

        int examinedCount =
            0;

        int sentCount =
            0;

        int rejectedCount =
            0;

        foreach (DiagnosticOutboxItem item
                 in pendingItems)
        {
            cancellationToken
                .ThrowIfCancellationRequested();

            examinedCount++;

            DiagnosticEnvelope? envelope =
                _outbox.TryRead(
                    item);

            if (envelope is null)
            {
                /*
                 * Corrupt/mismatched files are handled
                 * by DiagnosticOutbox itself.
                 */
                continue;
            }

            DiagnosticSendResult sendResult;

            try
            {
                sendResult =
                    await _sender.SendAsync(
                        envelope,
                        cancellationToken);
            }
            catch (OperationCanceledException)
                when (cancellationToken
                    .IsCancellationRequested)
            {
                throw;
            }
            catch
            {
                /*
                 * Defensive boundary: a sender
                 * implementation must never break app
                 * startup or normal execution.
                 */
                _logger.LogWarning(
                    DiagnosticEventIds
                        .DiagnosticDeliveryDeferred,
                    "Diagnostic delivery deferred.");

                return new DiagnosticProcessingResult(
                    examinedCount,
                    sentCount,
                    rejectedCount,
                    Deferred:
                        true);
            }

            switch (sendResult.Outcome)
            {
                case DiagnosticSendOutcome.Sent:
                    {
                        bool deleted =
                            _outbox.TryDelete(
                                item);

                        if (deleted)
                        {
                            sentCount++;

                            _logger.LogInformation(
                                DiagnosticEventIds
                                    .DiagnosticDeliverySent,
                                "Diagnostic delivered.");
                        }
                        else
                        {
                            _logger.LogWarning(
                                DiagnosticEventIds
                                    .DiagnosticDeliveryDeleteFailed,
                                "Delivered diagnostic could not be removed.");
                        }

                        break;
                    }

                case DiagnosticSendOutcome.Rejected:
                    {
                        bool quarantined =
                            _outbox.TryQuarantine(
                                item);

                        if (!quarantined)
                        {
                            _logger.LogWarning(
                                DiagnosticEventIds
                                    .DiagnosticDeliveryQuarantineFailed,
                                "Rejected diagnostic could not be quarantined.");

                            return new DiagnosticProcessingResult(
                                examinedCount,
                                sentCount,
                                rejectedCount,
                                Deferred:
                                    true);
                        }

                        rejectedCount++;

                        _logger.LogWarning(
                            DiagnosticEventIds
                                .DiagnosticDeliveryRejected,
                            "Diagnostic rejected.");

                        break;
                    }

                case DiagnosticSendOutcome.Disabled:
                case DiagnosticSendOutcome.RetryableFailure:
                case DiagnosticSendOutcome.ConfigurationFailure:
                default:
                    {
                        _logger.LogInformation(
                            DiagnosticEventIds
                                .DiagnosticDeliveryDeferred,
                            "Diagnostic delivery deferred.");

                        return new DiagnosticProcessingResult(
                            examinedCount,
                            sentCount,
                            rejectedCount,
                            Deferred:
                                true);
                    }
            }
        }

        return new DiagnosticProcessingResult(
            examinedCount,
            sentCount,
            rejectedCount,
            Deferred:
                false);
    }
}
