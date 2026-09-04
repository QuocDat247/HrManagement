using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace HrManagement.Desktop.Diagnostics;

public sealed class SafeFileLoggerProvider :
    ILoggerProvider
{
    private readonly object _syncRoot =
        new();

    private readonly DiagnosticLogOptions _options;

    private readonly string _sessionId =
        Guid.NewGuid().ToString(
            "N");

    private readonly string _applicationVersion;

    private readonly string _operatingSystem;

    private readonly string _framework;

    private bool _disposed;

    public SafeFileLoggerProvider(
        DiagnosticLogOptions options)
    {
        ArgumentNullException.ThrowIfNull(
            options);

        if (string.IsNullOrWhiteSpace(
                options.LogDirectory))
        {
            throw new ArgumentException(
                "Log directory is required.",
                nameof(options));
        }

        if (options.RetentionDays < 1)
        {
            throw new ArgumentOutOfRangeException(
                nameof(options));
        }

        if (options.MaxFileBytes < 1)
        {
            throw new ArgumentOutOfRangeException(
                nameof(options));
        }

        _options =
            options;

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

        TryDeleteExpiredFiles();
    }

    public ILogger CreateLogger(
        string categoryName)
    {
        return new SafeFileLogger(
            categoryName,
            Write);
    }

    public void Dispose()
    {
        _disposed =
            true;

        GC.SuppressFinalize(
            this);
    }

    private void Write(
        string categoryName,
        LogLevel logLevel,
        EventId eventId,
        Exception? exception)
    {
        if (_disposed)
        {
            return;
        }

        try
        {
            lock (_syncRoot)
            {
                Directory.CreateDirectory(
                    _options.LogDirectory);

                DateTime utcNow =
                    DateTime.UtcNow;

                string filePath =
                    GetWritableFilePath(
                        utcNow);

                SafeLogRecord record =
                    CreateRecord(
                        utcNow,
                        categoryName,
                        logLevel,
                        eventId,
                        exception);

                string json =
                    JsonSerializer.Serialize(
                        record,
                        JsonOptions);

                File.AppendAllText(
                    filePath,
                    json
                    + Environment.NewLine,
                    Encoding.UTF8);
            }
        }
        catch
        {
            /*
             * Diagnostics must never crash or block
             * the application.
             */
        }
    }

    private SafeLogRecord CreateRecord(
        DateTime utcNow,
        string categoryName,
        LogLevel logLevel,
        EventId eventId,
        Exception? exception)
    {
        return new SafeLogRecord(
            TimestampUtc:
                utcNow,
            Level:
                logLevel.ToString(),
            Category:
                categoryName,
            EventId:
                eventId.Id,
            EventName:
                eventId.Name
                ?? $"Event{eventId.Id}",
            SessionId:
                _sessionId,
            ApplicationVersion:
                _applicationVersion,
            OperatingSystem:
                _operatingSystem,
            Framework:
                _framework,
            Exception:
                CreateExceptionMetadata(
                    exception));
    }

    private static SafeExceptionMetadata?
        CreateExceptionMetadata(
            Exception? exception)
    {
        if (exception is null)
        {
            return null;
        }

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

        return new SafeExceptionMetadata(
            Type:
                exception.GetType().FullName
                ?? exception.GetType().Name,
            HResult:
                exception.HResult,
            StackTrace:
                exception.StackTrace,
            InnerExceptionTypes:
                innerTypes);
    }

    private string GetWritableFilePath(
        DateTime utcNow)
    {
        string date =
            utcNow.ToString(
                "yyyyMMdd");

        string basePath =
            Path.Combine(
                _options.LogDirectory,
                $"hrmanagement-{date}.jsonl");

        if (CanWriteTo(
                basePath))
        {
            return basePath;
        }

        for (int index = 1;
             index <= 999;
             index++)
        {
            string candidate =
                Path.Combine(
                    _options.LogDirectory,
                    $"hrmanagement-{date}-{index:000}.jsonl");

            if (CanWriteTo(
                    candidate))
            {
                return candidate;
            }
        }

        return Path.Combine(
            _options.LogDirectory,
            $"hrmanagement-{date}-{Guid.NewGuid():N}.jsonl");
    }

    private bool CanWriteTo(
        string filePath)
    {
        if (!File.Exists(
                filePath))
        {
            return true;
        }

        return
            new FileInfo(
                filePath)
            .Length
            < _options.MaxFileBytes;
    }

    private void TryDeleteExpiredFiles()
    {
        try
        {
            if (!Directory.Exists(
                    _options.LogDirectory))
            {
                return;
            }

            DateTime cutoffUtc =
                DateTime.UtcNow.AddDays(
                    -_options.RetentionDays);

            foreach (string filePath
                     in Directory.EnumerateFiles(
                         _options.LogDirectory,
                         "hrmanagement-*.jsonl"))
            {
                try
                {
                    DateTime lastWriteUtc =
                        File.GetLastWriteTimeUtc(
                            filePath);

                    if (lastWriteUtc
                        < cutoffUtc)
                    {
                        File.Delete(
                            filePath);
                    }
                }
                catch
                {
                    // Best-effort cleanup only.
                }
            }
        }
        catch
        {
            // Diagnostics cleanup must never stop startup.
        }
    }

    private static readonly
        JsonSerializerOptions JsonOptions =
            new()
            {
                PropertyNamingPolicy =
                    JsonNamingPolicy.CamelCase
            };

    private sealed record SafeLogRecord(
        DateTime TimestampUtc,
        string Level,
        string Category,
        int EventId,
        string EventName,
        string SessionId,
        string ApplicationVersion,
        string OperatingSystem,
        string Framework,
        SafeExceptionMetadata? Exception);

    private sealed record SafeExceptionMetadata(
        string Type,
        int HResult,
        string? StackTrace,
        IReadOnlyList<string> InnerExceptionTypes);
}
