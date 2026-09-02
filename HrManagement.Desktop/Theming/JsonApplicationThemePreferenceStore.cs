using System.IO;
using System.Text;
using System.Text.Json;

namespace HrManagement.Desktop.Theming;

public sealed class JsonApplicationThemePreferenceStore
    : IApplicationThemePreferenceStore
{
    private readonly string _filePath;

    public JsonApplicationThemePreferenceStore()
        : this(
            Path.Combine(
                Environment.GetFolderPath(
                    Environment.SpecialFolder.LocalApplicationData),
                "HrManagement",
                "theme-settings.json"))
    {
    }

    public JsonApplicationThemePreferenceStore(
        string filePath)
    {
        if (string.IsNullOrWhiteSpace(
                filePath))
        {
            throw new ArgumentException(
                "Đường dẫn lưu theme là bắt buộc.",
                nameof(filePath));
        }

        _filePath =
            filePath;
    }

    public async Task<ApplicationThemePreference?> LoadAsync(
        CancellationToken cancellationToken = default)
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
                JsonSerializer.Deserialize<StoredPreference>(
                    json);

            if (stored is null
                || !Enum.TryParse(
                    stored.Appearance,
                    ignoreCase: true,
                    out ApplicationAppearance appearance)
                || !Enum.TryParse(
                    stored.Accent,
                    ignoreCase: true,
                    out ApplicationAccent accent)
                || !Enum.IsDefined(
                    appearance)
                || !Enum.IsDefined(
                    accent))
            {
                return null;
            }

            return new ApplicationThemePreference(
                appearance,
                accent);
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
        ApplicationThemePreference preference,
        CancellationToken cancellationToken = default)
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
                preference.Appearance.ToString(),
                preference.Accent.ToString());

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
            overwrite: true);
    }

    private sealed record StoredPreference(
        string Appearance,
        string Accent);
}
