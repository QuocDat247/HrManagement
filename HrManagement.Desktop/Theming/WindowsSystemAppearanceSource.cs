using Microsoft.Win32;

namespace HrManagement.Desktop.Theming;

public sealed class WindowsSystemAppearanceSource
    : ISystemAppearanceSource
{
    private const string PersonalizeRegistryPath =
        @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize";

    public ApplicationAppearance GetCurrentAppearance()
    {
        try
        {
            using RegistryKey? key =
                Registry.CurrentUser.OpenSubKey(
                    PersonalizeRegistryPath);

            object? value =
                key?.GetValue(
                    "AppsUseLightTheme");

            if (value is int appsUseLightTheme)
            {
                return appsUseLightTheme == 0
                    ? ApplicationAppearance.Dark
                    : ApplicationAppearance.Light;
            }
        }
        catch
        {
            // Theme detection must never block application startup.
        }

        return ApplicationAppearance.Light;
    }
}
