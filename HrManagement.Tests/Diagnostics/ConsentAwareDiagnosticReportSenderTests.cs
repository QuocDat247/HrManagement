using System.Net;
using System.Net.Http;
using HrManagement.Desktop.Diagnostics;

namespace HrManagement.Tests.Diagnostics;

public sealed class
    ConsentAwareDiagnosticReportSenderTests
{
    [Fact]
    public async Task SendAsync_WhenConsentIsOff_DoesNotReachHttpTransport()
    {
        int httpRequestCount =
            0;

        using var httpClient =
            CreateHttpClient(
                (_, _) =>
                {
                    httpRequestCount++;

                    return Task.FromResult(
                        new HttpResponseMessage(
                            HttpStatusCode.Accepted));
                });

        var httpSender =
            CreateHttpSender(
                httpClient);

        var consentService =
            new StubDiagnosticConsentService(
                allowDiagnosticUpload:
                    false);

        var sender =
            new ConsentAwareDiagnosticReportSender(
                consentService,
                httpSender);

        DiagnosticSendResult result =
            await sender.SendAsync(
                CreateEnvelope());

        Assert.Equal(
            DiagnosticSendOutcome.NotAuthorized,
            result.Outcome);

        Assert.Equal(
            0,
            httpRequestCount);
    }

    [Fact]
    public async Task SendAsync_WhenConsentIsOn_DelegatesToHttpTransport()
    {
        int httpRequestCount =
            0;

        using var httpClient =
            CreateHttpClient(
                (_, _) =>
                {
                    httpRequestCount++;

                    return Task.FromResult(
                        new HttpResponseMessage(
                            HttpStatusCode.Accepted));
                });

        var httpSender =
            CreateHttpSender(
                httpClient);

        var consentService =
            new StubDiagnosticConsentService(
                allowDiagnosticUpload:
                    true);

        var sender =
            new ConsentAwareDiagnosticReportSender(
                consentService,
                httpSender);

        DiagnosticSendResult result =
            await sender.SendAsync(
                CreateEnvelope());

        Assert.Equal(
            DiagnosticSendOutcome.Sent,
            result.Outcome);

        Assert.Equal(
            1,
            httpRequestCount);
    }

    private static HttpDiagnosticReportSender
        CreateHttpSender(
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
                "DIAG-20260905-CONSENT1",
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
                        "CRASH-20260905-ABC123",
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

    private sealed class
        StubDiagnosticConsentService :
            IDiagnosticConsentService
    {
        public StubDiagnosticConsentService(
            bool allowDiagnosticUpload)
        {
            CurrentPreference =
                new DiagnosticConsentPreference(
                    allowDiagnosticUpload);
        }

        public DiagnosticConsentPreference
            CurrentPreference
        {
            get;
            private set;
        }

        public Task InitializeAsync(
            CancellationToken cancellationToken =
                default)
        {
            return Task.CompletedTask;
        }

        public Task ApplyAsync(
            DiagnosticConsentPreference preference,
            CancellationToken cancellationToken =
                default)
        {
            CurrentPreference =
                preference;

            return Task.CompletedTask;
        }
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
