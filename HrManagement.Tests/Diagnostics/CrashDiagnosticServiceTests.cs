using System.IO;
using HrManagement.Desktop.Diagnostics;

namespace HrManagement.Tests.Diagnostics;

public sealed class CrashDiagnosticServiceTests
{
    [Fact]
    public void TryCapture_WritesSafeCrashReport()
    {
        string directory =
            CreateTemporaryDirectory();

        try
        {
            var options =
                new CrashDiagnosticOptions(
                    directory,
                    RetentionDays:
                        30);

            var service =
                new CrashDiagnosticService(
                    options,
                    TimeProvider.System);

            const string sensitiveOuterMessage =
                "PRIVATE-EMPLOYEE-DATA";

            const string sensitiveInnerMessage =
                "SECRET-SALARY-DATA";

            Exception exception =
                CreateException(
                    sensitiveOuterMessage,
                    sensitiveInnerMessage);

            CrashDiagnosticResult? result =
                service.TryCapture(
                    exception,
                    CrashOrigin.DispatcherUnhandledException,
                    processTerminating:
                        false);

            Assert.NotNull(
                result);

            Assert.StartsWith(
                "CRASH-",
                result.CrashId);

            Assert.True(
                File.Exists(
                    result.FilePath));

            string content =
                File.ReadAllText(
                    result.FilePath);

            Assert.Contains(
                "DispatcherUnhandledException",
                content);

            Assert.Contains(
                "InvalidOperationException",
                content);

            Assert.Contains(
                "ArgumentException",
                content);

            Assert.DoesNotContain(
                sensitiveOuterMessage,
                content);

            Assert.DoesNotContain(
                sensitiveInnerMessage,
                content);
        }
        finally
        {
            DeleteDirectory(
                directory);
        }
    }

    [Fact]
    public void Constructor_RemovesExpiredReports()
    {
        string directory =
            CreateTemporaryDirectory();

        try
        {
            string expiredFile =
                Path.Combine(
                    directory,
                    "CRASH-20000101-ABCDEF.json");

            File.WriteAllText(
                expiredFile,
                "{}");

            File.SetLastWriteTimeUtc(
                expiredFile,
                DateTime.UtcNow.AddDays(
                    -60));

            var options =
                new CrashDiagnosticOptions(
                    directory,
                    RetentionDays:
                        30);

            _ =
                new CrashDiagnosticService(
                    options,
                    TimeProvider.System);

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
    public void TryCapture_CreatesUniqueCrashIds()
    {
        string directory =
            CreateTemporaryDirectory();

        try
        {
            var service =
                new CrashDiagnosticService(
                    new CrashDiagnosticOptions(
                        directory,
                        RetentionDays:
                            30),
                    TimeProvider.System);

            Exception exception =
                CreateException(
                    "outer",
                    "inner");

            CrashDiagnosticResult? first =
                service.TryCapture(
                    exception,
                    CrashOrigin.DispatcherUnhandledException,
                    false);

            CrashDiagnosticResult? second =
                service.TryCapture(
                    exception,
                    CrashOrigin.DispatcherUnhandledException,
                    false);

            Assert.NotNull(first);
            Assert.NotNull(second);

            Assert.NotEqual(
                first.CrashId,
                second.CrashId);
        }
        finally
        {
            DeleteDirectory(
                directory);
        }
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
