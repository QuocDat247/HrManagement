namespace HrManagement.Desktop.Diagnostics;

public sealed record DiagnosticTransportOptions(
    bool Enabled,
    Uri? Endpoint,
    TimeSpan Timeout)
{
    public static readonly TimeSpan DefaultTimeout =
        TimeSpan.FromSeconds(5);

    public static DiagnosticTransportOptions
        CreateDisabledDefault()
    {
        return new DiagnosticTransportOptions(
            Enabled:
                false,
            Endpoint:
                null,
            Timeout:
                DefaultTimeout);
    }
}
