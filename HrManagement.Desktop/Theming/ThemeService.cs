using System.Windows;

namespace HrManagement.Desktop.Theming;

public sealed class ThemeService : IThemeService
{
    private const string ThemePathPrefix =
        "/HrManagement.Desktop;component/Themes/Presets/";

    public AppTheme CurrentTheme { get; private set; }
        = AppTheme.Blue;

    public void ApplyTheme(AppTheme theme)
    {
        string themePath = theme switch
        {
            AppTheme.Blue =>
                $"{ThemePathPrefix}Blue.xaml",

            AppTheme.Green =>
                $"{ThemePathPrefix}Green.xaml",

            _ => throw new ArgumentOutOfRangeException(
                nameof(theme),
                theme,
                "Theme is not supported.")
        };

        ResourceDictionary resources =
            System.Windows.Application.Current.Resources;

        ResourceDictionary? existingTheme =
            resources.MergedDictionaries
                .FirstOrDefault(IsThemeDictionary);

        int existingIndex = -1;

        if (existingTheme is not null)
        {
            existingIndex =
                resources.MergedDictionaries.IndexOf(existingTheme);

            resources.MergedDictionaries.Remove(existingTheme);
        }

        var newThemeDictionary = new ResourceDictionary
        {
            Source = new Uri(
                themePath,
                UriKind.RelativeOrAbsolute)
        };

        if (existingIndex >= 0)
        {
            resources.MergedDictionaries.Insert(
                existingIndex,
                newThemeDictionary);
        }
        else
        {
            resources.MergedDictionaries.Insert(
                0,
                newThemeDictionary);
        }

        CurrentTheme = theme;
    }

    private static bool IsThemeDictionary(
        ResourceDictionary dictionary)
    {
        string? source =
            dictionary.Source?.OriginalString;

        if (string.IsNullOrWhiteSpace(source))
        {
            return false;
        }

        string normalizedSource =
            source.Replace('\\', '/');

        return normalizedSource.Contains(
            "Themes/Presets/",
            StringComparison.OrdinalIgnoreCase);
    }
}