using System.IO;

namespace HrManagement.Desktop.Diagnostics;

public sealed record CrashDiagnosticOptions(
    string CrashDirectory,
    int RetentionDays)
{
    public const int DefaultRetentionDays = 30;

    public static CrashDiagnosticOptions CreateDefault()
    {
        string localApplicationData =
            Environment.GetFolderPath(
                Environment.SpecialFolder.LocalApplicationData);

        return new CrashDiagnosticOptions(
            Path.Combine(
                localApplicationData,
                "HrManagement",
                "crashes"),
            DefaultRetentionDays);
    }
}
