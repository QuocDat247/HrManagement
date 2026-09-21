using System.IO;

namespace HrManagement.Desktop.Diagnostics;

public static class DiagnosticPrivacySanitizer
{
    public static string? SanitizeStackTrace(
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
                Path.GetTempPath()
                    .TrimEnd(
                        Path.DirectorySeparatorChar,
                        Path.AltDirectorySeparatorChar),
                "%TEMP%");

        sanitized =
            ReplacePath(
                sanitized,
                Environment.GetFolderPath(
                    Environment.SpecialFolder.LocalApplicationData),
                "%LOCALAPPDATA%");

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
}
