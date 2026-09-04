using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.Json;

namespace HrManagement.Desktop.Diagnostics;

public sealed class CrashDiagnosticService :
    ICrashDiagnosticService
{
    private readonly object _syncRoot =
        new();

    private readonly CrashDiagnosticOptions _options;

    private readonly TimeProvider _timeProvider;

    private readonly string _applicationVersion;

    private readonly string _operatingSystem;

    private readonly string _framework;

    public CrashDiagnosticService(
        CrashDiagnosticOptions options,
        TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(
            options);

        ArgumentNullException.ThrowIfNull(
            timeProvider);

        if (string.IsNullOrWhiteSpace(
                options.CrashDirectory))
        {
            throw new ArgumentException(
                "Crash directory is required.",
                nameof(options));
        }

        if (options.RetentionDays < 1)
        {
            throw new ArgumentOutOfRangeException(
                nameof(options));
        }

        _options =
            options;

        _timeProvider =
            timeProvider;

        _applicationVersion =
            Assembly.GetEntryAssembly()
                ?.GetName()
                .Version
                ?.ToString()
            ?? "unknown";

        _operatingSystem =
            RuntimeInformation.OSDescription;

        _framework =
            RuntimeInformation.FrameworkDescription;

        TryDeleteExpiredReports();
    }

    public CrashDiagnosticResult? TryCapture(
        Exception exception,
        CrashOrigin origin,
        bool processTerminating)
    {
        ArgumentNullException.ThrowIfNull(
            exception);

        try
        {
            lock (_syncRoot)
            {
                Directory.CreateDirectory(
                    _options.CrashDirectory);

                DateTime utcNow =
                    _timeProvider
                        .GetUtcNow()
                        .UtcDateTime;

                string crashId =
                    CreateCrashId(
                        utcNow);

                string filePath =
                    Path.Combine(
                        _options.CrashDirectory,
                        $"{crashId}.json");

                CrashDiagnosticDocument report =
                    new(
                        SchemaVersion:
                            1,
                        CrashId:
                            crashId,
                        TimestampUtc:
                            utcNow,
                        Origin:
                            origin.ToString(),
                        ProcessTerminating:
                            processTerminating,
                        ApplicationVersion:
                            _applicationVersion,
                        OperatingSystem:
                            _operatingSystem,
                        Framework:
                            _framework,
                        Exception:
                            CreateExceptionMetadata(
                                exception));

                string json =
                    JsonSerializer.Serialize(
                        report,
                        JsonOptions);

                File.WriteAllText(
                    filePath,
                    json);

                return new CrashDiagnosticResult(
                    crashId,
                    filePath,
                    report);
            }
        }
        catch
        {
            /*
             * Crash reporting itself must never
             * create another application failure.
             */
            return null;
        }
    }

    private static string CreateCrashId(
        DateTime utcNow)
    {
        string suffix =
            Guid.NewGuid()
                .ToString("N")[..6]
                .ToUpperInvariant();

        return
            $"CRASH-{utcNow:yyyyMMdd}-{suffix}";
    }

    private static SafeExceptionDiagnostic
        CreateExceptionMetadata(
            Exception exception)
    {
        var innerTypes =
            new List<string>();

        Exception? current =
            exception.InnerException;

        while (current is not null)
        {
            innerTypes.Add(
                current.GetType().FullName
                ?? current.GetType().Name);

            current =
                current.InnerException;
        }

        return new SafeExceptionDiagnostic(
            Type:
                exception.GetType().FullName
                ?? exception.GetType().Name,
            HResult:
                exception.HResult,
            StackTrace:
                SanitizeStackTrace(
                    exception.StackTrace),
            InnerExceptionTypes:
                innerTypes);
    }

    private static string? SanitizeStackTrace(
        string? stackTrace)
    {
        if (string.IsNullOrWhiteSpace(
                stackTrace))
        {
            return stackTrace;
        }

        string sanitized =
            stackTrace;

        sanitized =
            ReplacePath(
                sanitized,
                Environment.GetFolderPath(
                    Environment.SpecialFolder.LocalApplicationData),
                "%LOCALAPPDATA%");

        sanitized =
            ReplacePath(
                sanitized,
                Path.GetTempPath()
                    .TrimEnd(
                        Path.DirectorySeparatorChar,
                        Path.AltDirectorySeparatorChar),
                "%TEMP%");

        sanitized =
            ReplacePath(
                sanitized,
                Environment.GetFolderPath(
                    Environment.SpecialFolder.UserProfile),
                "%USERPROFILE%");

        return sanitized;
    }

    private static string ReplacePath(
        string value,
        string path,
        string replacement)
    {
        if (string.IsNullOrWhiteSpace(
                path))
        {
            return value;
        }

        return value.Replace(
            path,
            replacement,
            StringComparison.OrdinalIgnoreCase);
    }

    private void TryDeleteExpiredReports()
    {
        try
        {
            if (!Directory.Exists(
                    _options.CrashDirectory))
            {
                return;
            }

            DateTime cutoffUtc =
                _timeProvider
                    .GetUtcNow()
                    .UtcDateTime
                    .AddDays(
                        -_options.RetentionDays);

            foreach (string filePath
                     in Directory.EnumerateFiles(
                         _options.CrashDirectory,
                         "CRASH-*.json"))
            {
                try
                {
                    if (File.GetLastWriteTimeUtc(
                            filePath)
                        < cutoffUtc)
                    {
                        File.Delete(
                            filePath);
                    }
                }
                catch
                {
                    // Best-effort cleanup.
                }
            }
        }
        catch
        {
            // Cleanup must never block startup.
        }
    }

    private static readonly
        JsonSerializerOptions JsonOptions =
            new()
            {
                PropertyNamingPolicy =
                    JsonNamingPolicy.CamelCase,

                WriteIndented =
                    true
            };
}
