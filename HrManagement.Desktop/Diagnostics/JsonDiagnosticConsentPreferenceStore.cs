using System.IO;
using System.Text;
using System.Text.Json;

namespace HrManagement.Desktop.Diagnostics;

public sealed class JsonDiagnosticConsentPreferenceStore :
    IDiagnosticConsentPreferenceStore
{
    private readonly string _filePath;

    public JsonDiagnosticConsentPreferenceStore()
        : this(
            Path.Combine(
                Environment.GetFolderPath(
                    Environment.SpecialFolder.LocalApplicationData),
                "HrManagement",
                "diagnostic-consent.json"))
    {
    }

    public JsonDiagnosticConsentPreferenceStore(
        string filePath)
    {
        if (string.IsNullOrWhiteSpace(
                filePath))
        {
            throw new ArgumentException(
                "Diagnostic consent path is required.",
                nameof(filePath));
        }

        _filePath =
            filePath;
    }

    public async Task<DiagnosticConsentPreference?>
        LoadAsync(
            CancellationToken cancellationToken =
                default)
    {
        if (!File.Exists(
                _filePath))
        {
            return null;
        }

        try
        {
            string json =
                await File.ReadAllTextAsync(
                    _filePath,
                    cancellationToken);

            StoredPreference? stored =
                JsonSerializer.Deserialize<
                    StoredPreference>(
                    json);

            if (stored is null)
            {
                return null;
            }

            return new DiagnosticConsentPreference(
                stored.AllowDiagnosticUpload);
        }
        catch (JsonException)
        {
            return null;
        }
        catch (IOException)
        {
            return null;
        }
        catch (UnauthorizedAccessException)
        {
            return null;
        }
    }

    public async Task SaveAsync(
        DiagnosticConsentPreference preference,
        CancellationToken cancellationToken =
            default)
    {
        ArgumentNullException.ThrowIfNull(
            preference);

        string? directory =
            Path.GetDirectoryName(
                _filePath);

        if (!string.IsNullOrWhiteSpace(
                directory))
        {
            Directory.CreateDirectory(
                directory);
        }

        var stored =
            new StoredPreference(
                preference.AllowDiagnosticUpload);

        string json =
            JsonSerializer.Serialize(
                stored,
                new JsonSerializerOptions
                {
                    WriteIndented =
                        true
                });

        string temporaryFilePath =
            _filePath
            + ".tmp";

        await File.WriteAllTextAsync(
            temporaryFilePath,
            json,
            Encoding.UTF8,
            cancellationToken);

        File.Move(
            temporaryFilePath,
            _filePath,
            overwrite:
                true);
    }

    private sealed record StoredPreference(
        bool AllowDiagnosticUpload);
}
