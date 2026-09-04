using System.IO;

namespace HrManagement.Desktop.Diagnostics;

public sealed record DiagnosticOutboxOptions(
    string OutboxDirectory,
    string QuarantineDirectory,
    int RetentionDays)
{
    public const int DefaultRetentionDays = 30;

    public static DiagnosticOutboxOptions CreateDefault()
    {
        string localApplicationData =
            Environment.GetFolderPath(
                Environment.SpecialFolder.LocalApplicationData);

        string diagnosticsDirectory =
            Path.Combine(
                localApplicationData,
                "HrManagement",
                "diagnostics");

        return new DiagnosticOutboxOptions(
            Path.Combine(
                diagnosticsDirectory,
                "outbox"),
            Path.Combine(
                diagnosticsDirectory,
                "quarantine"),
            DefaultRetentionDays);
    }
}
