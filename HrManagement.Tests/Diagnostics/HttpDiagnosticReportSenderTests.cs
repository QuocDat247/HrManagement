using System.Net;
using System.Net.Http;
using HrManagement.Desktop.Diagnostics;

namespace HrManagement.Tests.Diagnostics;

public sealed class
    HttpDiagnosticReportSenderTests
{
    [Fact]
    public async Task SendAsync_WhenDisabled_DoesNotSend()
    {
        int requestCount =
            0;

        using var httpClient =
            CreateHttpClient(
                (_, _) =>
                {
                    requestCount++;

                    return Task.FromResult(
                        new HttpResponseMessage(
                            HttpStatusCode.OK));
                });

        var sender =
            new HttpDiagnosticReportSender(
                httpClient,
                DiagnosticTransportOptions
                    .CreateDisabledDefault());

        DiagnosticSendResult result =
            await sender.SendAsync(
                CreateEnvelope());

        Assert.Equal(
            DiagnosticSendOutcome.Disabled,
            result.Outcome);

        Assert.Equal(
            0,
            requestCount);
    }

    [Fact]
    public async Task SendAsync_WithHttpsEndpoint_PostsEnvelope()
    {
        string? requestBody =
            null;

        string? diagnosticIdHeader =
            null;

        HttpMethod? method =
            null;

        Uri? requestUri =
            null;

        using var httpClient =
            CreateHttpClient(
                async (
                    request,
                    _) =>
                {
                    method =
                        request.Method;

                    requestUri =
                        request.RequestUri;

                    diagnosticIdHeader =
                        request.Headers
                            .TryGetValues(
                                "X-Diagnostic-Id",
                                out IEnumerable<string>?
                                    values)
                            ? Assert.Single(
                                values)
                            : null;

                    requestBody =
                        request.Content is null
                            ? null
                            : await request.Content
                                .ReadAsStringAsync();

                    return new HttpResponseMessage(
                        HttpStatusCode.Accepted);
                });

        var sender =
            new HttpDiagnosticReportSender(
                httpClient,
                new DiagnosticTransportOptions(
                    Enabled:
                        true,
                    Endpoint:
                        new Uri(
                            "https://diagnostics.example.test/api/v1/reports"),
                    Timeout:
                        TimeSpan.FromSeconds(
                            5)));

        DiagnosticEnvelope envelope =
            CreateEnvelope();

        DiagnosticSendResult result =
            await sender.SendAsync(
                envelope);

        Assert.Equal(
            DiagnosticSendOutcome.Sent,
            result.Outcome);

        Assert.Equal(
            202,
            result.HttpStatusCode);

        Assert.Equal(
            HttpMethod.Post,
            method);

        Assert.Equal(
            "https://diagnostics.example.test/api/v1/reports",
            requestUri?.AbsoluteUri);

        Assert.Equal(
            envelope.DiagnosticId,
            diagnosticIdHeader);

        Assert.NotNull(
            requestBody);

        Assert.Contains(
            envelope.DiagnosticId,
            requestBody);

        Assert.DoesNotContain(
            "\"message\"",
            requestBody.ToLowerInvariant());
    }

    [Fact]
    public async Task SendAsync_WithHttpEndpoint_ReturnsConfigurationFailure()
    {
        int requestCount =
            0;

        using var httpClient =
            CreateHttpClient(
                (_, _) =>
                {
                    requestCount++;

                    return Task.FromResult(
                        new HttpResponseMessage(
                            HttpStatusCode.OK));
                });

        var sender =
            new HttpDiagnosticReportSender(
                httpClient,
                new DiagnosticTransportOptions(
                    Enabled:
                        true,
                    Endpoint:
                        new Uri(
                            "http://diagnostics.example.test/reports"),
                    Timeout:
                        TimeSpan.FromSeconds(
                            5)));

        DiagnosticSendResult result =
            await sender.SendAsync(
                CreateEnvelope());

        Assert.Equal(
            DiagnosticSendOutcome.ConfigurationFailure,
            result.Outcome);

        Assert.Equal(
            0,
            requestCount);
    }

    [Theory]
    [InlineData(408)]
    [InlineData(425)]
    [InlineData(429)]
    [InlineData(500)]
    [InlineData(503)]
    public async Task SendAsync_RetryableStatus_ReturnsRetryableFailure(
        int statusCode)
    {
        using var httpClient =
            CreateHttpClient(
                (_, _) =>
                    Task.FromResult(
                        new HttpResponseMessage(
                            (HttpStatusCode)
                                statusCode)));

        var sender =
            CreateEnabledSender(
                httpClient);

        DiagnosticSendResult result =
            await sender.SendAsync(
                CreateEnvelope());

        Assert.Equal(
            DiagnosticSendOutcome
                .RetryableFailure,
            result.Outcome);

        Assert.Equal(
            statusCode,
            result.HttpStatusCode);
    }

    [Fact]
    public async Task SendAsync_BadRequest_ReturnsRejected()
    {
        using var httpClient =
            CreateHttpClient(
                (_, _) =>
                    Task.FromResult(
                        new HttpResponseMessage(
                            HttpStatusCode.BadRequest)));

        var sender =
            CreateEnabledSender(
                httpClient);

        DiagnosticSendResult result =
            await sender.SendAsync(
                CreateEnvelope());

        Assert.Equal(
            DiagnosticSendOutcome.Rejected,
            result.Outcome);

        Assert.Equal(
            400,
            result.HttpStatusCode);
    }

    [Fact]
    public async Task SendAsync_Unauthorized_KeepsDiagnosticPending()
    {
        using var httpClient =
            CreateHttpClient(
                (_, _) =>
                    Task.FromResult(
                        new HttpResponseMessage(
                            HttpStatusCode.Unauthorized)));

        var sender =
            CreateEnabledSender(
                httpClient);

        DiagnosticSendResult result =
            await sender.SendAsync(
                CreateEnvelope());

        Assert.Equal(
            DiagnosticSendOutcome.ConfigurationFailure,
            result.Outcome);

        Assert.Equal(
            401,
            result.HttpStatusCode);
    }

    [Fact]
    public async Task SendAsync_WhenCallerCancels_PropagatesCancellation()
    {
        using var httpClient =
            CreateHttpClient(
                async (
                    _,
                    cancellationToken) =>
                {
                    await Task.Delay(
                        Timeout.InfiniteTimeSpan,
                        cancellationToken);

                    throw new InvalidOperationException();
                });

        var sender =
            CreateEnabledSender(
                httpClient);

        using var cancellation =
            new CancellationTokenSource();

        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<
            OperationCanceledException>(
            () =>
                sender.SendAsync(
                    CreateEnvelope(),
                    cancellation.Token));
    }

    [Fact]
    public async Task SendAsync_NetworkFailure_ReturnsRetryableFailure()
    {
        using var httpClient =
            CreateHttpClient(
                (_, _) =>
                    throw new HttpRequestException(
                        "TEST-ONLY"));

        var sender =
            CreateEnabledSender(
                httpClient);

        DiagnosticSendResult result =
            await sender.SendAsync(
                CreateEnvelope());

        Assert.Equal(
            DiagnosticSendOutcome
                .RetryableFailure,
            result.Outcome);
    }

    private static HttpDiagnosticReportSender
        CreateEnabledSender(
            HttpClient httpClient)
    {
        return new HttpDiagnosticReportSender(
            httpClient,
            new DiagnosticTransportOptions(
                Enabled:
                    true,
                Endpoint:
                    new Uri(
                        "https://diagnostics.example.test/api/v1/reports"),
                Timeout:
                    TimeSpan.FromSeconds(
                        5)));
    }

    private static HttpClient CreateHttpClient(
        Func<
            HttpRequestMessage,
            CancellationToken,
            Task<HttpResponseMessage>>
            callback)
    {
        return new HttpClient(
            new StubHttpMessageHandler(
                callback));
    }

    private static DiagnosticEnvelope
        CreateEnvelope()
    {
        return new DiagnosticEnvelope(
            SchemaVersion:
                1,
            DiagnosticId:
                "DIAG-20260904-ABCDEF12",
            Kind:
                "Crash",
            CreatedAtUtc:
                DateTime.UtcNow,
            Application:
                new DiagnosticApplicationMetadata(
                    Version:
                        "1.0.0",
                    OperatingSystem:
                        "Windows",
                    Framework:
                        ".NET"),
            Crash:
                new DiagnosticCrashPayload(
                    CrashId:
                        "CRASH-20260904-ABC123",
                    TimestampUtc:
                        DateTime.UtcNow,
                    Origin:
                        "DispatcherUnhandledException",
                    ProcessTerminating:
                        false,
                    ExceptionType:
                        "System.InvalidOperationException",
                    HResult:
                        -1,
                    StackTrace:
                        "safe stack trace",
                    InnerExceptionTypes:
                        Array.Empty<string>()));
    }

    private sealed class StubHttpMessageHandler :
        HttpMessageHandler
    {
        private readonly Func<
            HttpRequestMessage,
            CancellationToken,
            Task<HttpResponseMessage>>
            _callback;

        public StubHttpMessageHandler(
            Func<
                HttpRequestMessage,
                CancellationToken,
                Task<HttpResponseMessage>>
                callback)
        {
            _callback =
                callback;
        }

        protected override Task<HttpResponseMessage>
            SendAsync(
                HttpRequestMessage request,
                CancellationToken cancellationToken)
        {
            return _callback(
                request,
                cancellationToken);
        }
    }
}
