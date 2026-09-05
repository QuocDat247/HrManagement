using System.IO;
using HrManagement.Desktop.Diagnostics;

namespace HrManagement.Tests.Diagnostics;

public sealed class DiagnosticOutboxTests
{
    [Fact]
    public void TryEnqueue_PersistsAndReadsEnvelope()
    {
        TestDirectories directories =
            CreateDirectories();

        try
        {
            var outbox =
                CreateOutbox(
                    directories);

            DiagnosticEnvelope envelope =
                CreateEnvelope(
                    "DIAG-20260904-ABCDEF12");

            DiagnosticOutboxItem? item =
                outbox.TryEnqueue(
                    envelope);

            Assert.NotNull(item);
            Assert.True(
                File.Exists(
                    item.FilePath));

            DiagnosticEnvelope? restored =
                outbox.TryRead(
                    item);

            Assert.NotNull(restored);

            Assert.Equal(
                envelope.DiagnosticId,
                restored.DiagnosticId);

            Assert.Equal(
                envelope.Crash.CrashId,
                restored.Crash.CrashId);
        }
        finally
        {
            DeleteDirectory(
                directories.Root);
        }
    }

    [Fact]
    public void TryEnqueue_WithSameId_IsIdempotent()
    {
        TestDirectories directories =
            CreateDirectories();

        try
        {
            var outbox =
                CreateOutbox(
                    directories);

            DiagnosticEnvelope envelope =
                CreateEnvelope(
                    "DIAG-20260904-ABCDEF12");

            Assert.NotNull(
                outbox.TryEnqueue(
                    envelope));

            Assert.NotNull(
                outbox.TryEnqueue(
                    envelope));

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
    public void TryRead_CorruptFile_MovesItToQuarantine()
    {
        TestDirectories directories =
            CreateDirectories();

        try
        {
            Directory.CreateDirectory(
                directories.Outbox);

            string filePath =
                Path.Combine(
                    directories.Outbox,
                    "DIAG-20260904-CORRUPT1.json");

            File.WriteAllText(
                filePath,
                "{ not valid json");

            var outbox =
                CreateOutbox(
                    directories);

            DiagnosticOutboxItem item =
                Assert.Single(
                    outbox.GetPendingItems());

            Assert.Null(
                outbox.TryRead(
                    item));

            Assert.False(
                File.Exists(
                    filePath));

            Assert.Single(
                Directory.GetFiles(
                    directories.Quarantine,
                    "*.json"));
        }
        finally
        {
            DeleteDirectory(
                directories.Root);
        }
    }

    [Fact]
    public void TryDelete_RemovesPendingItem()
    {
        TestDirectories directories =
            CreateDirectories();

        try
        {
            var outbox =
                CreateOutbox(
                    directories);

            DiagnosticOutboxItem? item =
                outbox.TryEnqueue(
                    CreateEnvelope(
                        "DIAG-20260904-DELETE01"));

            Assert.NotNull(item);

            Assert.True(
                outbox.TryDelete(
                    item));

            Assert.Empty(
                outbox.GetPendingItems());
        }
        finally
        {
            DeleteDirectory(
                directories.Root);
        }
    }

    [Fact]
    public void TryQuarantine_MovesPendingItem()
    {
        TestDirectories directories =
            CreateDirectories();

        try
        {
            var outbox =
                CreateOutbox(
                    directories);

            DiagnosticOutboxItem? item =
                outbox.TryEnqueue(
                    CreateEnvelope(
                        "DIAG-20260905-QUARANT1"));

            Assert.NotNull(item);

            Assert.True(
                outbox.TryQuarantine(
                    item));

            Assert.Empty(
                outbox.GetPendingItems());

            Assert.Single(
                Directory.GetFiles(
                    directories.Quarantine,
                    "*.json"));
        }
        finally
        {
            DeleteDirectory(
                directories.Root);
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
        string Outbox,
        string Quarantine);
}
