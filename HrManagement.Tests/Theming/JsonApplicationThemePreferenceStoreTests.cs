using HrManagement.Desktop.Theming;

namespace HrManagement.Tests.Theming;

public sealed class JsonApplicationThemePreferenceStoreTests
{
    [Fact]
    public async Task SaveAndLoadAsync_RoundTripsPreference()
    {
        string directory =
            CreateTemporaryDirectory();

        try
        {
            string filePath =
                Path.Combine(
                    directory,
                    "theme-settings.json");

            var store =
                new JsonApplicationThemePreferenceStore(
                    filePath);

            var preference =
                new ApplicationThemePreference(
                    ApplicationAppearance.Dark,
                    ApplicationAccent.Green);

            await store.SaveAsync(
                preference);

            ApplicationThemePreference? loaded =
                await store.LoadAsync();

            Assert.Equal(
                preference,
                loaded);

            Assert.True(
                File.Exists(
                    filePath));
        }
        finally
        {
            DeleteDirectory(
                directory);
        }
    }

    [Fact]
    public async Task LoadAsync_WhenFileDoesNotExist_ReturnsNull()
    {
        string directory =
            CreateTemporaryDirectory();

        try
        {
            string filePath =
                Path.Combine(
                    directory,
                    "missing.json");

            var store =
                new JsonApplicationThemePreferenceStore(
                    filePath);

            ApplicationThemePreference? loaded =
                await store.LoadAsync();

            Assert.Null(
                loaded);
        }
        finally
        {
            DeleteDirectory(
                directory);
        }
    }

    [Fact]
    public async Task LoadAsync_WhenJsonIsInvalid_ReturnsNull()
    {
        string directory =
            CreateTemporaryDirectory();

        try
        {
            string filePath =
                Path.Combine(
                    directory,
                    "theme-settings.json");

            await File.WriteAllTextAsync(
                filePath,
                "{ not-valid-json");

            var store =
                new JsonApplicationThemePreferenceStore(
                    filePath);

            ApplicationThemePreference? loaded =
                await store.LoadAsync();

            Assert.Null(
                loaded);
        }
        finally
        {
            DeleteDirectory(
                directory);
        }
    }

    [Fact]
    public async Task LoadAsync_WhenEnumValueIsUnsupported_ReturnsNull()
    {
        string directory =
            CreateTemporaryDirectory();

        try
        {
            string filePath =
                Path.Combine(
                    directory,
                    "theme-settings.json");

            await File.WriteAllTextAsync(
                filePath,
                """
                {
                  "Appearance": "Neon",
                  "Accent": "Blue"
                }
                """);

            var store =
                new JsonApplicationThemePreferenceStore(
                    filePath);

            ApplicationThemePreference? loaded =
                await store.LoadAsync();

            Assert.Null(
                loaded);
        }
        finally
        {
            DeleteDirectory(
                directory);
        }
    }

    private static string CreateTemporaryDirectory()
    {
        string directory =
            Path.Combine(
                Path.GetTempPath(),
                "HrManagement.Tests",
                Guid.NewGuid().ToString(
                    "N"));

        Directory.CreateDirectory(
            directory);

        return directory;
    }

    private static void DeleteDirectory(
        string directory)
    {
        if (!Directory.Exists(
                directory))
        {
            return;
        }

        Directory.Delete(
            directory,
            recursive:
                true);
    }
}
