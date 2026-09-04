using HrManagement.Desktop.Diagnostics;
using Microsoft.Extensions.Logging;

namespace HrManagement.Tests.Diagnostics;

public sealed class SafeFileLoggerProviderTests
{
    [Fact]
    public void Log_DoesNotPersistStateOrExceptionMessage()
    {
        string directory =
            CreateTemporaryDirectory();

        try
        {
            var options =
                new DiagnosticLogOptions(
                    directory,
                    RetentionDays:
                        14,
                    MaxFileBytes:
                        1024 * 1024);

            using var provider =
                new SafeFileLoggerProvider(
                    options);

            ILogger logger =
                provider.CreateLogger(
                    "SafetyTest");

            const string sensitiveState =
                "EMP001-PRIVATE";

            const string sensitiveMessage =
                "SECRET-EXCEPTION-MESSAGE";

            var exception =
                new InvalidOperationException(
                    sensitiveMessage);

            logger.Log(
                LogLevel.Error,
                new EventId(
                    4321,
                    "SaveFailed"),
                sensitiveState,
                exception,
                static (
                    state,
                    _) =>
                    $"Sensitive state: {state}");

            string filePath =
                Assert.Single(
                    Directory.GetFiles(
                        directory,
                        "*.jsonl"));

            string content =
                File.ReadAllText(
                    filePath);

            Assert.Contains(
                "SaveFailed",
                content);

            Assert.Contains(
                "InvalidOperationException",
                content);

            Assert.DoesNotContain(
                sensitiveState,
                content);

            Assert.DoesNotContain(
                sensitiveMessage,
                content);

            Assert.DoesNotContain(
                "Sensitive state",
                content);
        }
        finally
        {
            DeleteDirectory(
                directory);
        }
    }

    [Fact]
    public void Constructor_RemovesExpiredLogFiles()
    {
        string directory =
            CreateTemporaryDirectory();

        try
        {
            string expiredFile =
                Path.Combine(
                    directory,
                    "hrmanagement-20000101.jsonl");

            File.WriteAllText(
                expiredFile,
                "{}");

            File.SetLastWriteTimeUtc(
                expiredFile,
                DateTime.UtcNow.AddDays(
                    -30));

            var options =
                new DiagnosticLogOptions(
                    directory,
                    RetentionDays:
                        14,
                    MaxFileBytes:
                        1024 * 1024);

            using var provider =
                new SafeFileLoggerProvider(
                    options);

            Assert.False(
                File.Exists(
                    expiredFile));
        }
        finally
        {
            DeleteDirectory(
                directory);
        }
    }

    [Fact]
    public void Log_WhenCurrentFileIsFull_RollsToNextFile()
    {
        string directory =
            CreateTemporaryDirectory();

        try
        {
            string date =
                DateTime.UtcNow.ToString(
                    "yyyyMMdd");

            string fullFile =
                Path.Combine(
                    directory,
                    $"hrmanagement-{date}.jsonl");

            File.WriteAllText(
                fullFile,
                new string(
                    'x',
                    100));

            var options =
                new DiagnosticLogOptions(
                    directory,
                    RetentionDays:
                        14,
                    MaxFileBytes:
                        50);

            using var provider =
                new SafeFileLoggerProvider(
                    options);

            ILogger logger =
                provider.CreateLogger(
                    "RolloverTest");

            logger.LogInformation(
                new EventId(
                    1234,
                    "RolloverEvent"),
                "Test");

            string rolledFile =
                Path.Combine(
                    directory,
                    $"hrmanagement-{date}-001.jsonl");

            Assert.True(
                File.Exists(
                    rolledFile));
        }
        finally
        {
            DeleteDirectory(
                directory);
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
        if (!Directory.Exists(
                directory))
        {
            return;
        }

        Directory.Delete(
            directory,
            recursive:
                true);
    }
}
