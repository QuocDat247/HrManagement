using System.IO;
using HrManagement.Desktop.Diagnostics;

namespace HrManagement.Tests.Diagnostics;

public sealed class
    JsonDiagnosticConsentPreferenceStoreTests
{
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
                    "consent.json");

            var store =
                new JsonDiagnosticConsentPreferenceStore(
                    filePath);

            DiagnosticConsentPreference? result =
                await store.LoadAsync();

            Assert.Null(
                result);
        }
        finally
        {
            DeleteDirectory(
                directory);
        }
    }

    [Fact]
    public async Task SaveAndLoadAsync_PreservesConsent()
    {
        string directory =
            CreateTemporaryDirectory();

        try
        {
            string filePath =
                Path.Combine(
                    directory,
                    "consent.json");

            var store =
                new JsonDiagnosticConsentPreferenceStore(
                    filePath);

            await store.SaveAsync(
                new DiagnosticConsentPreference(
                    AllowDiagnosticUpload:
                        true));

            DiagnosticConsentPreference? result =
                await store.LoadAsync();

            Assert.NotNull(
                result);

            Assert.True(
                result.AllowDiagnosticUpload);
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
                    "consent.json");

            await File.WriteAllTextAsync(
                filePath,
                "{ invalid json");

            var store =
                new JsonDiagnosticConsentPreferenceStore(
                    filePath);

            DiagnosticConsentPreference? result =
                await store.LoadAsync();

            Assert.Null(
                result);
        }
        finally
        {
            DeleteDirectory(
                directory);
        }
    }

    [Fact]
    public void DefaultPreference_DoesNotAllowUpload()
    {
        Assert.False(
            DiagnosticConsentPreference
                .Default
                .AllowDiagnosticUpload);
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
        if (Directory.Exists(
                directory))
        {
            Directory.Delete(
                directory,
                recursive:
                    true);
        }
    }
}
