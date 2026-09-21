using System.IO;
using HrManagement.Desktop.Diagnostics;

namespace HrManagement.Tests.Diagnostics;

public sealed class DiagnosticPrivacySanitizerTests
{
    [Fact]
    public void SanitizeStackTrace_ReplacesSensitiveUserPaths()
    {
        string localApplicationData =
            Environment.GetFolderPath(
                Environment.SpecialFolder.LocalApplicationData);

        string userProfile =
            Environment.GetFolderPath(
                Environment.SpecialFolder.UserProfile);

        string tempPath =
            Path.GetTempPath()
                .TrimEnd(
                    Path.DirectorySeparatorChar,
                    Path.AltDirectorySeparatorChar);

        string stackTrace =
            string.Join(
                Environment.NewLine,
                $"at App.Start() in {localApplicationData}\\HrManagement\\App.cs:line 10",
                $"at Temp.Work() in {tempPath}\\probe.cs:line 20",
                $"at User.Work() in {userProfile}\\Documents\\probe.cs:line 30");

        string? sanitized =
            DiagnosticPrivacySanitizer
                .SanitizeStackTrace(
                    stackTrace);

        Assert.NotNull(
            sanitized);

        Assert.Contains(
            "%LOCALAPPDATA%",
            sanitized);

        Assert.Contains(
            "%TEMP%",
            sanitized);

        Assert.Contains(
            "%USERPROFILE%",
            sanitized);

        Assert.DoesNotContain(
            localApplicationData,
            sanitized,
            StringComparison.OrdinalIgnoreCase);

        Assert.DoesNotContain(
            tempPath,
            sanitized,
            StringComparison.OrdinalIgnoreCase);

        Assert.DoesNotContain(
            userProfile,
            sanitized,
            StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void SanitizeStackTrace_WhenEmpty_ReturnsOriginal(
        string? stackTrace)
    {
        string? sanitized =
            DiagnosticPrivacySanitizer
                .SanitizeStackTrace(
                    stackTrace);

        Assert.Equal(
            stackTrace,
            sanitized);
    }
}
