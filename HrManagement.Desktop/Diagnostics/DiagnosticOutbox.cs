using System.IO;
using System.Text;

namespace HrManagement.Desktop.Diagnostics;

public sealed class DiagnosticOutbox :
    IDiagnosticOutbox
{
    private readonly object _syncRoot =
        new();

    private readonly DiagnosticOutboxOptions _options;

    private readonly TimeProvider _timeProvider;

    private static readonly UTF8Encoding Utf8NoBom =
        new(
            encoderShouldEmitUTF8Identifier:
                false);

    public DiagnosticOutbox(
        DiagnosticOutboxOptions options,
        TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(
            options);

        ArgumentNullException.ThrowIfNull(
            timeProvider);

        if (string.IsNullOrWhiteSpace(
                options.OutboxDirectory))
        {
            throw new ArgumentException(
                "Outbox directory is required.",
                nameof(options));
        }

        if (string.IsNullOrWhiteSpace(
                options.QuarantineDirectory))
        {
            throw new ArgumentException(
                "Quarantine directory is required.",
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

        TryDeleteExpiredFiles();
    }

    public DiagnosticOutboxItem? TryEnqueue(
        DiagnosticEnvelope envelope)
    {
        ArgumentNullException.ThrowIfNull(
            envelope);

        if (!IsSafeDiagnosticId(
                envelope.DiagnosticId))
        {
            return null;
        }

        string? temporaryPath =
            null;

        try
        {
            lock (_syncRoot)
            {
                Directory.CreateDirectory(
                    _options.OutboxDirectory);

                string finalPath =
                    GetOutboxPath(
                        envelope.DiagnosticId);

                /*
                 * Same DiagnosticId is treated as
                 * idempotent. Never overwrite it.
                 */
                if (File.Exists(
                        finalPath))
                {
                    return new DiagnosticOutboxItem(
                        envelope.DiagnosticId,
                        finalPath);
                }

                temporaryPath =
                    Path.Combine(
                        _options.OutboxDirectory,
                        $".{envelope.DiagnosticId}"
                        + $".{Guid.NewGuid():N}.tmp");

                string json =
                    DiagnosticEnvelopeJson.Serialize(
                        envelope);

                File.WriteAllText(
                    temporaryPath,
                    json,
                    Utf8NoBom);

                /*
                 * Rename on the same volume gives us
                 * an atomic publication boundary.
                 */
                File.Move(
                    temporaryPath,
                    finalPath);

                temporaryPath =
                    null;

                return new DiagnosticOutboxItem(
                    envelope.DiagnosticId,
                    finalPath);
            }
        }
        catch
        {
            TryDeleteFile(
                temporaryPath);

            return null;
        }
    }

    public IReadOnlyList<DiagnosticOutboxItem>
        GetPendingItems()
    {
        try
        {
            lock (_syncRoot)
            {
                if (!Directory.Exists(
                        _options.OutboxDirectory))
                {
                    return Array.Empty<
                        DiagnosticOutboxItem>();
                }

                return Directory
                    .EnumerateFiles(
                        _options.OutboxDirectory,
                        "DIAG-*.json",
                        SearchOption.TopDirectoryOnly)
                    .OrderBy(
                        File.GetLastWriteTimeUtc)
                    .Select(
                        filePath =>
                            new DiagnosticOutboxItem(
                                Path.GetFileNameWithoutExtension(
                                    filePath),
                                filePath))
                    .ToArray();
            }
        }
        catch
        {
            return Array.Empty<
                DiagnosticOutboxItem>();
        }
    }

    public DiagnosticEnvelope? TryRead(
        DiagnosticOutboxItem item)
    {
        ArgumentNullException.ThrowIfNull(
            item);

        try
        {
            lock (_syncRoot)
            {
                if (!IsExpectedOutboxItem(
                        item))
                {
                    return null;
                }

                string json =
                    File.ReadAllText(
                        item.FilePath,
                        Encoding.UTF8);

                DiagnosticEnvelope envelope =
                    DiagnosticEnvelopeJson.Deserialize(
                        json);

                if (!string.Equals(
                        envelope.DiagnosticId,
                        item.DiagnosticId,
                        StringComparison.Ordinal))
                {
                    TryMoveToQuarantineCore(
                        item);

                    return null;
                }

                return envelope;
            }
        }
        catch
        {
            lock (_syncRoot)
            {
                TryMoveToQuarantineCore(
                    item);
            }

            return null;
        }
    }

    public bool TryDelete(
        DiagnosticOutboxItem item)
    {
        ArgumentNullException.ThrowIfNull(
            item);

        try
        {
            lock (_syncRoot)
            {
                if (!IsExpectedOutboxItem(
                        item))
                {
                    return false;
                }

                if (File.Exists(
                        item.FilePath))
                {
                    File.Delete(
                        item.FilePath);
                }

                return true;
            }
        }
        catch
        {
            return false;
        }
    }

    public bool TryQuarantine(
    DiagnosticOutboxItem item)
    {
        ArgumentNullException.ThrowIfNull(
            item);

        try
        {
            lock (_syncRoot)
            {
                if (!IsExpectedOutboxItem(
                        item))
                {
                    return false;
                }

                return TryMoveToQuarantineCore(
                    item);
            }
        }
        catch
        {
            return false;
        }
    }

    private bool IsExpectedOutboxItem(
        DiagnosticOutboxItem item)
    {
        if (!IsSafeDiagnosticId(
                item.DiagnosticId))
        {
            return false;
        }

        string expectedPath =
            Path.GetFullPath(
                GetOutboxPath(
                    item.DiagnosticId));

        string actualPath =
            Path.GetFullPath(
                item.FilePath);

        return string.Equals(
            expectedPath,
            actualPath,
            StringComparison.OrdinalIgnoreCase);
    }

    private string GetOutboxPath(
        string diagnosticId)
    {
        return Path.Combine(
            _options.OutboxDirectory,
            $"{diagnosticId}.json");
    }

    private bool TryMoveToQuarantineCore(
    DiagnosticOutboxItem item)
    {
        try
        {
            if (!File.Exists(
                    item.FilePath))
            {
                return true;
            }

            Directory.CreateDirectory(
                _options.QuarantineDirectory);

            string destinationPath =
                Path.Combine(
                    _options.QuarantineDirectory,
                    $"{item.DiagnosticId}.json");

            if (File.Exists(
                    destinationPath))
            {
                destinationPath =
                    Path.Combine(
                        _options.QuarantineDirectory,
                        $"{item.DiagnosticId}"
                        + $"-{Guid.NewGuid():N}.json");
            }

            File.Move(
                item.FilePath,
                destinationPath);

            return true;
        }
        catch
        {
            return false;
        }
    }

    private static bool IsSafeDiagnosticId(
        string diagnosticId)
    {
        if (string.IsNullOrWhiteSpace(
                diagnosticId)
            || diagnosticId.Length > 80
            || !diagnosticId.StartsWith(
                "DIAG-",
                StringComparison.Ordinal))
        {
            return false;
        }

        return diagnosticId.All(
            character =>
                char.IsLetterOrDigit(
                    character)
                || character == '-');
    }

    private void TryDeleteExpiredFiles()
    {
        try
        {
            DateTime cutoffUtc =
                _timeProvider
                    .GetUtcNow()
                    .UtcDateTime
                    .AddDays(
                        -_options.RetentionDays);

            DeleteExpiredFiles(
                _options.OutboxDirectory,
                cutoffUtc);

            DeleteExpiredFiles(
                _options.QuarantineDirectory,
                cutoffUtc);
        }
        catch
        {
            // Maintenance must never block startup.
        }
    }

    private static void DeleteExpiredFiles(
        string directory,
        DateTime cutoffUtc)
    {
        if (!Directory.Exists(
                directory))
        {
            return;
        }

        foreach (string filePath
                 in Directory.EnumerateFiles(
                     directory,
                     "*.json",
                     SearchOption.TopDirectoryOnly))
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

    private static void TryDeleteFile(
        string? filePath)
    {
        if (string.IsNullOrWhiteSpace(
                filePath))
        {
            return;
        }

        try
        {
            if (File.Exists(
                    filePath))
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
