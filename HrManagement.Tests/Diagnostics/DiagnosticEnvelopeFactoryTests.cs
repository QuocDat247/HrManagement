using System.IO;
using HrManagement.Desktop.Diagnostics;

namespace HrManagement.Tests.Diagnostics;

public sealed class DiagnosticEnvelopeFactoryTests
{
    [Fact]
    public void Create_FromCapturedCrash_ProducesSafeEnvelope()
    {
        string directory =
            CreateTemporaryDirectory();

        try
        {
            const string sensitiveOuterMessage =
                "PRIVATE-EMPLOYEE-NAME";

            const string sensitiveInnerMessage =
                "SECRET-SALARY-VALUE";

            Exception exception =
                CreateException(
                    sensitiveOuterMessage,
                    sensitiveInnerMessage);

            var crashService =
                new CrashDiagnosticService(
                    new CrashDiagnosticOptions(
                        directory,
                        RetentionDays:
                            30),
                    TimeProvider.System);

            CrashDiagnosticResult? crashResult =
                crashService.TryCapture(
                    exception,
                    CrashOrigin.DispatcherUnhandledException,
                    processTerminating:
                        false);

            Assert.NotNull(
                crashResult);

            var factory =
                new DiagnosticEnvelopeFactory(
                    TimeProvider.System);

            DiagnosticEnvelope envelope =
                factory.Create(
                    crashResult.Document);

            string json =
                DiagnosticEnvelopeJson.Serialize(
                    envelope);

            Assert.Equal(
                1,
                envelope.SchemaVersion);

            Assert.Equal(
                "Crash",
                envelope.Kind);

            Assert.StartsWith(
                "DIAG-",
                envelope.DiagnosticId);

            Assert.Equal(
                crashResult.CrashId,
                envelope.Crash.CrashId);

            Assert.Contains(
                "InvalidOperationException",
                json);

            Assert.Contains(
                "ArgumentException",
                json);

            Assert.DoesNotContain(
                sensitiveOuterMessage,
                json);

            Assert.DoesNotContain(
                sensitiveInnerMessage,
                json);

            string lowerJson =
                json.ToLowerInvariant();

            Assert.DoesNotContain(
                "\"message\"",
                lowerJson);

            Assert.DoesNotContain(
                "\"username\"",
                lowerJson);

            Assert.DoesNotContain(
                "\"machinename\"",
                lowerJson);

            Assert.DoesNotContain(
                "\"employeeid\"",
                lowerJson);

            Assert.DoesNotContain(
                "\"employeecode\"",
                lowerJson);

            Assert.DoesNotContain(
                "\"email\"",
                lowerJson);
        }
        finally
        {
            DeleteDirectory(
                directory);
        }
    }

    [Fact]
    public void Create_WhenCrashSchemaIsUnsupported_Throws()
    {
        var factory =
            new DiagnosticEnvelopeFactory(
                TimeProvider.System);

        var document =
            new CrashDiagnosticDocument(
                SchemaVersion:
                    999,
                CrashId:
                    "CRASH-TEST-ABCDEF",
                TimestampUtc:
                    DateTime.UtcNow,
                Origin:
                    "DispatcherUnhandledException",
                ProcessTerminating:
                    false,
                ApplicationVersion:
                    "1.0.0",
                OperatingSystem:
                    "Windows",
                Framework:
                    ".NET",
                Exception:
                    new SafeExceptionDiagnostic(
                        Type:
                            "System.Exception",
                        HResult:
                            -1,
                        StackTrace:
                            null,
                        InnerExceptionTypes:
                            Array.Empty<string>()));

        Assert.Throws<NotSupportedException>(
            () =>
                factory.Create(
                    document));
    }

    private static Exception CreateException(
        string outerMessage,
        string innerMessage)
    {
        try
        {
            throw new InvalidOperationException(
                outerMessage,
                new ArgumentException(
                    innerMessage));
        }
        catch (Exception exception)
        {
            return exception;
        }
    }

    private static string CreateTemporaryDirectory()
    {
        string directory =
            Path.Combine(
                Path.GetTempPath(),
                "HrManagement.Tests",
                Guid.NewGuid().ToString(
                    "N"));

        Directory.CreateDirectory(
            directory);

        return directory;
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
}
