using System.Net;
using HrManagement.Desktop.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;

namespace HrManagement.Tests.Diagnostics;

public sealed class DiagnosticOutboxProcessorTests
{
    [Fact]
    public async Task ProcessPendingAsync_WhenSent_DeletesItem()
    {
        TestContext context =
            CreateContext(
                new DiagnosticSendResult(
                    DiagnosticSendOutcome.Sent,
                    (int)HttpStatusCode.Accepted));

        try
        {
            context.Outbox.TryEnqueue(
                CreateEnvelope(
                    "DIAG-20260905-SENT0001"));

            DiagnosticProcessingResult result =
                await context.Processor
                    .ProcessPendingAsync();

            Assert.Equal(
                1,
                context.Sender.SendCallCount);

            Assert.Equal(
                1,
                result.SentCount);

            Assert.False(
                result.Deferred);

            Assert.Empty(
                context.Outbox
                    .GetPendingItems());
        }
        finally
        {
            DeleteDirectory(
                context.Root);
        }
    }

    [Fact]
    public async Task ProcessPendingAsync_WhenRetryable_KeepsItem()
    {
        TestContext context =
            CreateContext(
                new DiagnosticSendResult(
                    DiagnosticSendOutcome
                        .RetryableFailure,
                    503));

        try
        {
            context.Outbox.TryEnqueue(
                CreateEnvelope(
                    "DIAG-20260905-RETRY001"));

            DiagnosticProcessingResult result =
                await context.Processor
                    .ProcessPendingAsync();

            Assert.True(
                result.Deferred);

            Assert.Single(
                context.Outbox
                    .GetPendingItems());
        }
        finally
        {
            DeleteDirectory(
                context.Root);
        }
    }

    [Fact]
    public async Task ProcessPendingAsync_WhenConfigurationFails_KeepsItem()
    {
        TestContext context =
            CreateContext(
                new DiagnosticSendResult(
                    DiagnosticSendOutcome
                        .ConfigurationFailure,
                    401));

        try
        {
            context.Outbox.TryEnqueue(
                CreateEnvelope(
                    "DIAG-20260905-CONFIG01"));

            DiagnosticProcessingResult result =
                await context.Processor
                    .ProcessPendingAsync();

            Assert.True(
                result.Deferred);

            Assert.Single(
                context.Outbox
                    .GetPendingItems());
        }
        finally
        {
            DeleteDirectory(
                context.Root);
        }
    }

    [Fact]
    public async Task ProcessPendingAsync_WhenRejected_QuarantinesItem()
    {
        TestContext context =
            CreateContext(
                new DiagnosticSendResult(
                    DiagnosticSendOutcome.Rejected,
                    400));

        try
        {
            context.Outbox.TryEnqueue(
                CreateEnvelope(
                    "DIAG-20260905-REJECT01"));

            DiagnosticProcessingResult result =
                await context.Processor
                    .ProcessPendingAsync();

            Assert.Equal(
                1,
                result.RejectedCount);

            Assert.False(
                result.Deferred);

            Assert.Empty(
                context.Outbox
                    .GetPendingItems());

            Assert.Single(
                Directory.GetFiles(
                    context.Quarantine,
                    "*.json"));
        }
        finally
        {
            DeleteDirectory(
                context.Root);
        }
    }

    [Fact]
    public async Task ProcessPendingAsync_WhenDisabled_KeepsItem()
    {
        TestContext context =
            CreateContext(
                new DiagnosticSendResult(
                    DiagnosticSendOutcome.Disabled));

        try
        {
            context.Outbox.TryEnqueue(
                CreateEnvelope(
                    "DIAG-20260905-DISABLED"));

            DiagnosticProcessingResult result =
                await context.Processor
                    .ProcessPendingAsync();

            Assert.True(
                result.Deferred);

            Assert.Single(
                context.Outbox
                    .GetPendingItems());
        }
        finally
        {
            DeleteDirectory(
                context.Root);
        }
    }

    [Fact]
    public async Task ProcessPendingAsync_WhenConsentIsOff_DoesNotSend()
    {
        TestContext context =
            CreateContext(
                new DiagnosticSendResult(
                    DiagnosticSendOutcome.Sent,
                    202),
                allowDiagnosticUpload:
                    false);

        try
        {
            context.Outbox.TryEnqueue(
                CreateEnvelope(
                    "DIAG-20260905-NOCONS01"));

            DiagnosticProcessingResult result =
                await context.Processor
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
                context.Sender.SendCallCount);

            Assert.Single(
                context.Outbox
                    .GetPendingItems());
        }
        finally
        {
            DeleteDirectory(
                context.Root);
        }
    }

    private static TestContext CreateContext(
        DiagnosticSendResult sendResult,
        bool allowDiagnosticUpload =
            true)
    {
        string root =
            Path.Combine(
                Path.GetTempPath(),
                "HrManagement.Tests",
                Guid.NewGuid().ToString(
                    "N"));

        string outboxDirectory =
            Path.Combine(
                root,
                "outbox");

        string quarantineDirectory =
            Path.Combine(
                root,
                "quarantine");

        var outbox =
            new DiagnosticOutbox(
                new DiagnosticOutboxOptions(
                    outboxDirectory,
                    quarantineDirectory,
                    RetentionDays:
                        30),
                TimeProvider.System);

        var sender =
            new StubDiagnosticReportSender(
                sendResult);

        var consentService =
            new StubDiagnosticConsentService(
                allowDiagnosticUpload);

        var processor =
            new DiagnosticOutboxProcessor(
                outbox,
                sender,
                consentService,
                new DiagnosticOutboxProcessorOptions(
                    MaxItemsPerRun:
                        10),
                NullLogger<
                    DiagnosticOutboxProcessor>
                    .Instance);

        return new TestContext(
            root,
            quarantineDirectory,
            outbox,
            processor,
            sender);
    }

    private static DiagnosticEnvelope CreateEnvelope(
        string diagnosticId)
    {
        return new DiagnosticEnvelope(
            SchemaVersion:
                1,
            DiagnosticId:
                diagnosticId,
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

    private sealed record TestContext(
        string Root,
        string Quarantine,
        DiagnosticOutbox Outbox,
        DiagnosticOutboxProcessor Processor,
        StubDiagnosticReportSender Sender);

    private sealed class StubDiagnosticConsentService :
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

    private sealed class StubDiagnosticReportSender :
        IDiagnosticReportSender
    {
        public int SendCallCount
        {
            get;
            private set;
        }

        private readonly DiagnosticSendResult
            _result;

        public StubDiagnosticReportSender(
            DiagnosticSendResult result)
        {
            _result =
                result;
        }

        public Task<DiagnosticSendResult>
            SendAsync(
                DiagnosticEnvelope envelope,
                CancellationToken cancellationToken =
                    default)
        {
            SendCallCount++;

            cancellationToken
                .ThrowIfCancellationRequested();

            return Task.FromResult(
                _result);
        }
    }
}
