using System.IO;

namespace HrManagement.Desktop.Diagnostics;

public sealed record DiagnosticLogOptions(
    string LogDirectory,
    int RetentionDays,
    long MaxFileBytes)
{
    public const int DefaultRetentionDays = 14;

    public const long DefaultMaxFileBytes =
        5L * 1024L * 1024L;

    public static DiagnosticLogOptions CreateDefault()
    {
        string localApplicationData =
            Environment.GetFolderPath(
                Environment.SpecialFolder.LocalApplicationData);

        return new DiagnosticLogOptions(
            Path.Combine(
                localApplicationData,
                "HrManagement",
                "logs"),
            DefaultRetentionDays,
            DefaultMaxFileBytes);
    }
}
