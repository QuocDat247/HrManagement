using Microsoft.Extensions.Logging;

namespace HrManagement.Desktop.Diagnostics;

internal sealed class SafeFileLogger : ILogger
{
    private readonly string _categoryName;

    private readonly Action<
        string,
        LogLevel,
        EventId,
        Exception?> _write;

    public SafeFileLogger(
        string categoryName,
        Action<
            string,
            LogLevel,
            EventId,
            Exception?> write)
    {
        _categoryName =
            categoryName
            ?? throw new ArgumentNullException(
                nameof(categoryName));

        _write =
            write
            ?? throw new ArgumentNullException(
                nameof(write));
    }

    public IDisposable? BeginScope<TState>(
        TState state)
        where TState : notnull
    {
        return null;
    }

    public bool IsEnabled(
        LogLevel logLevel)
    {
        return
            logLevel >= LogLevel.Information
            && logLevel != LogLevel.None;
    }

    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        if (!IsEnabled(
                logLevel))
        {
            return;
        }

        /*
         * Intentionally do NOT persist:
         *
         * - state
         * - formatter output
         * - message arguments
         *
         * They may contain sensitive HR information.
         */

        _write(
            _categoryName,
            logLevel,
            eventId,
            exception);
    }
}
