using System.Text.Json;

namespace HrManagement.Desktop.Diagnostics;

public static class DiagnosticEnvelopeJson
{
    private static readonly
        JsonSerializerOptions Options =
            new()
            {
                PropertyNamingPolicy =
                    JsonNamingPolicy.CamelCase
            };

    public static string Serialize(
        DiagnosticEnvelope envelope)
    {
        ArgumentNullException.ThrowIfNull(
            envelope);

        return JsonSerializer.Serialize(
            envelope,
            Options);
    }
}
