namespace HrManagement.Desktop.Diagnostics;

public sealed record DiagnosticOutboxProcessorOptions(
    int MaxItemsPerRun)
{
    public const int DefaultMaxItemsPerRun = 10;

    public static DiagnosticOutboxProcessorOptions
        CreateDefault()
    {
        return new DiagnosticOutboxProcessorOptions(
            DefaultMaxItemsPerRun);
    }
}
