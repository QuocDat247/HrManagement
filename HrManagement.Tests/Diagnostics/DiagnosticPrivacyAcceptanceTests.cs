using System.IO;
using System.Net;
using System.Net.Http;
using HrManagement.Desktop.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;

namespace HrManagement.Tests.Diagnostics;

public sealed class DiagnosticPrivacyAcceptanceTests
{
    [Fact]
    public async Task ConsentOff_KeepsSafeReportLocal_AndNeverReachesHttp()
    {
        TestDirectories directories =
            CreateDirectories();

        try
        {
            DiagnosticEnvelope envelope =
                CreateSafeEnvelope(
                    directories);

            var outbox =
                CreateOutbox(
                    directories);

            DiagnosticOutboxItem item =
                Assert.IsType<DiagnosticOutboxItem>(
                    outbox.TryEnqueue(
                        envelope));

            string queuedJson =
                File.ReadAllText(
                    item.FilePath);

            Assert.DoesNotContain(
                "PRIVATE-EMPLOYEE-NAME",
                queuedJson);

            Assert.DoesNotContain(
                "SECRET-SALARY-VALUE",
                queuedJson);

            int httpRequestCount =
                0;

            using HttpClient httpClient =
                CreateHttpClient(
                    (_, _) =>
                    {
                        httpRequestCount++;

                        return Task.FromResult(
                            new HttpResponseMessage(
                                HttpStatusCode.Accepted));
                    });

            var consentService =
                await CreateConsentServiceAsync(
                    allowDiagnosticUpload:
                        false);

            var processor =
                CreateProcessor(
                    outbox,
                    consentService,
                    httpClient);

            DiagnosticProcessingResult result =
                await processor
                    .ProcessPendingAsync();

            Assert.True(
                result.Deferred);

            Assert.Equal(
                0,
                result.ExaminedCount);

            Assert.Equal(
                0,
                result.SentCount);

            Assert.Equal(
                0,
                httpRequestCount);

            Assert.Single(
                outbox.GetPendingItems());
        }
        finally
        {
            DeleteDirectory(
                directories.Root);
        }
    }

    [Fact]
    public async Task ConsentOn_SendsOnlyAllowlistedEnvelope()
    {
        TestDirectories directories =
            CreateDirectories();

        try
        {
            DiagnosticEnvelope envelope =
                CreateSafeEnvelope(
                    directories);

            var outbox =
                CreateOutbox(
                    directories);

            Assert.NotNull(
                outbox.TryEnqueue(
                    envelope));

            int httpRequestCount =
                0;

            string? requestBody =
                null;

            using HttpClient httpClient =
                CreateHttpClient(
                    async (
                        request,
                        _) =>
                    {
                        httpRequestCount++;

                        requestBody =
                            request.Content is null
                                ? null
                                : await request.Content
                                    .ReadAsStringAsync();

                        return new HttpResponseMessage(
                            HttpStatusCode.Accepted);
                    });

            var consentService =
                await CreateConsentServiceAsync(
                    allowDiagnosticUpload:
                        true);

            var processor =
                CreateProcessor(
                    outbox,
                    consentService,
                    httpClient);

            DiagnosticProcessingResult result =
                await processor
                    .ProcessPendingAsync();

            Assert.False(
                result.Deferred);

            Assert.Equal(
                1,
                result.SentCount);

            Assert.Equal(
                1,
                httpRequestCount);

            Assert.Empty(
                outbox.GetPendingItems());

            Assert.NotNull(
                requestBody);

            Assert.Contains(
                envelope.DiagnosticId,
                requestBody);

            Assert.DoesNotContain(
                "PRIVATE-EMPLOYEE-NAME",
                requestBody);

            Assert.DoesNotContain(
                "SECRET-SALARY-VALUE",
                requestBody);

            string lowerBody =
                requestBody.ToLowerInvariant();

            Assert.DoesNotContain(
                "\"message\"",
                lowerBody);

            Assert.DoesNotContain(
                "\"username\"",
                lowerBody);

            Assert.DoesNotContain(
                "\"machinename\"",
                lowerBody);

            Assert.DoesNotContain(
                "\"employeeid\"",
                lowerBody);

            Assert.DoesNotContain(
                "\"employeecode\"",
                lowerBody);

            Assert.DoesNotContain(
                "\"email\"",
                lowerBody);
        }
        finally
        {
            DeleteDirectory(
                directories.Root);
        }
    }

    private static DiagnosticEnvelope CreateSafeEnvelope(
        TestDirectories directories)
    {
        var crashService =
            new CrashDiagnosticService(
                new CrashDiagnosticOptions(
                    directories.Crashes,
                    RetentionDays:
                        30),
                TimeProvider.System);

        Exception exception =
            CreateException();

        CrashDiagnosticResult crashResult =
            Assert.IsType<CrashDiagnosticResult>(
                crashService.TryCapture(
                    exception,
                    CrashOrigin.DispatcherUnhandledException,
                    processTerminating:
                        false));

        var factory =
            new DiagnosticEnvelopeFactory(
                TimeProvider.System);

        return factory.Create(
            crashResult.Document);
    }

    private static Exception CreateException()
    {
        try
        {
            throw new InvalidOperationException(
                "PRIVATE-EMPLOYEE-NAME",
                new ArgumentException(
                    "SECRET-SALARY-VALUE"));
        }
        catch (Exception exception)
        {
            return exception;
        }
    }

    private static DiagnosticOutbox CreateOutbox(
        TestDirectories directories)
    {
        return new DiagnosticOutbox(
            new DiagnosticOutboxOptions(
                directories.Outbox,
                directories.Quarantine,
                RetentionDays:
                    30),
            TimeProvider.System);
    }

    private static async Task<DiagnosticConsentService>
        CreateConsentServiceAsync(
            bool allowDiagnosticUpload)
    {
        var store =
            new StubConsentStore(
                new DiagnosticConsentPreference(
                    allowDiagnosticUpload));

        var service =
            new DiagnosticConsentService(
                store);

        await service.InitializeAsync();

        return service;
    }

    private static DiagnosticOutboxProcessor CreateProcessor(
        DiagnosticOutbox outbox,
        IDiagnosticConsentService consentService,
        HttpClient httpClient)
    {
        var httpSender =
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

        var sender =
            new ConsentAwareDiagnosticReportSender(
                consentService,
                httpSender);

        return new DiagnosticOutboxProcessor(
            outbox,
            sender,
            consentService,
            new DiagnosticOutboxProcessorOptions(
                MaxItemsPerRun:
                    10),
            NullLogger<
                DiagnosticOutboxProcessor>
                .Instance);
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

    private static TestDirectories CreateDirectories()
    {
        string root =
            Path.Combine(
                Path.GetTempPath(),
                "HrManagement.Tests",
                Guid.NewGuid().ToString(
                    "N"));

        return new TestDirectories(
            root,
            Path.Combine(
                root,
                "crashes"),
            Path.Combine(
                root,
                "outbox"),
            Path.Combine(
                root,
                "quarantine"));
    }

    private static void DeleteDirectory(
        string directory)
    {
        if (Directory.Exists(
                directory))
        {
            Directory.Delete(
                directory,
                recursive:
                    true);
        }
    }

    private sealed record TestDirectories(
        string Root,
        string Crashes,
        string Outbox,
        string Quarantine);

    private sealed class StubConsentStore :
        IDiagnosticConsentPreferenceStore
    {
        private DiagnosticConsentPreference?
            _preference;

        public StubConsentStore(
            DiagnosticConsentPreference?
                preference)
        {
            _preference =
                preference;
        }

        public Task<DiagnosticConsentPreference?>
            LoadAsync(
                CancellationToken cancellationToken =
                    default)
        {
            return Task.FromResult(
                _preference);
        }

        public Task SaveAsync(
            DiagnosticConsentPreference preference,
            CancellationToken cancellationToken =
                default)
        {
            _preference =
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
