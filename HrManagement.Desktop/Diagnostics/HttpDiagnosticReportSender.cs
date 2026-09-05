using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;

namespace HrManagement.Desktop.Diagnostics;

public sealed class HttpDiagnosticReportSender :
    IDiagnosticReportSender
{
    private readonly HttpClient _httpClient;

    private readonly DiagnosticTransportOptions
        _options;

    public HttpDiagnosticReportSender(
        HttpClient httpClient,
        DiagnosticTransportOptions options)
    {
        ArgumentNullException.ThrowIfNull(
            httpClient);

        ArgumentNullException.ThrowIfNull(
            options);

        _httpClient =
            httpClient;

        _options =
            options;
    }

    public async Task<DiagnosticSendResult> SendAsync(
    DiagnosticEnvelope envelope,
    CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(envelope);

        if (!_options.Enabled)
        {
            return new DiagnosticSendResult(DiagnosticSendOutcome.Disabled);
        }

        if (!IsValidHttpsEndpoint(_options.Endpoint) || _options.Timeout <= TimeSpan.Zero)
        {
            return new DiagnosticSendResult(DiagnosticSendOutcome.ConfigurationFailure);
        }

        try
        {
            string json = DiagnosticEnvelopeJson.Serialize(envelope);

            using var request = new HttpRequestMessage(HttpMethod.Post, _options.Endpoint);

            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            request.Headers.TryAddWithoutValidation("X-Diagnostic-Id", envelope.DiagnosticId);

            request.Content = new StringContent(json, Encoding.UTF8, "application/json");

            using var timeoutCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutCancellation.CancelAfter(_options.Timeout);

            using HttpResponseMessage response = await _httpClient.SendAsync(
                request,
                HttpCompletionOption.ResponseHeadersRead,
                timeoutCancellation.Token);

            int statusCode = (int)response.StatusCode;

            if (response.IsSuccessStatusCode)
            {
                return new DiagnosticSendResult(DiagnosticSendOutcome.Sent, statusCode);
            }

            if (IsRetryableStatusCode(response.StatusCode))
            {
                return new DiagnosticSendResult(DiagnosticSendOutcome.RetryableFailure, statusCode);
            }

            if (IsRejectedStatusCode(response.StatusCode))
            {
                return new DiagnosticSendResult(DiagnosticSendOutcome.Rejected, statusCode);
            }

            /*
             * Authentication, authorization, endpoint deployment,
             * method/version mismatch, etc. may be repaired later.
             * Keep the envelope pending rather than discarding it.
             */
            return new DiagnosticSendResult(DiagnosticSendOutcome.ConfigurationFailure, statusCode);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Khi caller yêu cầu hủy thao tác -> rethrow exception
            throw;
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            /*
             * Diagnostic transport timeout.
             * Outbox item must remain pending.
             */
            return new DiagnosticSendResult(DiagnosticSendOutcome.RetryableFailure);
        }
        catch (HttpRequestException)
        {
            /*
             * DNS, TLS, connection failure,
             * server unavailable, offline...
             */
            return new DiagnosticSendResult(DiagnosticSendOutcome.RetryableFailure);
        }
        catch
        {
            /*
             * Unknown client-side failure must never cause
             * a queued diagnostic to be discarded.
             */
            return new DiagnosticSendResult(DiagnosticSendOutcome.ConfigurationFailure);
        }
    }

    private static bool IsValidHttpsEndpoint(
        Uri? endpoint)
    {
        return
            endpoint is not null
            && endpoint.IsAbsoluteUri
            && string.Equals(
                endpoint.Scheme,
                Uri.UriSchemeHttps,
                StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsRetryableStatusCode(
        HttpStatusCode statusCode)
    {
        int numericStatusCode =
            (int)statusCode;

        return
            statusCode
                == HttpStatusCode.RequestTimeout
            || numericStatusCode == 425
            || numericStatusCode == 429
            || numericStatusCode >= 500;
    }

    private static bool IsRejectedStatusCode(
    HttpStatusCode statusCode)
    {
        int numericStatusCode =
            (int)statusCode;

        return
            numericStatusCode == 400
            || numericStatusCode == 413
            || numericStatusCode == 415
            || numericStatusCode == 422;
    }
}
