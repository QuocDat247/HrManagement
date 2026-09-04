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

    public static DiagnosticEnvelope Deserialize(
    string json)
    {
        if (string.IsNullOrWhiteSpace(
                json))
        {
            throw new ArgumentException(
                "Diagnostic JSON is required.",
                nameof(json));
        }

        DiagnosticEnvelope? envelope =
            JsonSerializer.Deserialize<
                DiagnosticEnvelope>(
                json,
                Options);

        return envelope
            ?? throw new JsonException(
                "Diagnostic envelope is empty.");
    }

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
