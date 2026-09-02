using System.Windows;

namespace HrManagement.Desktop.Theming;

public sealed class ApplicationThemeService
    : IApplicationThemeService
{
    private const string AppearancePathPrefix =
        "/HrManagement.Desktop;component/Themes/Appearance/";

    private const string AccentPathPrefix =
        "/HrManagement.Desktop;component/Themes/Accents/";

    private readonly IApplicationThemePreferenceStore
        _preferenceStore;

    private readonly ISystemAppearanceSource
        _systemAppearanceSource;

    public ApplicationThemeService(
        IApplicationThemePreferenceStore preferenceStore,
        ISystemAppearanceSource systemAppearanceSource)
    {
        _preferenceStore =
            preferenceStore;

        _systemAppearanceSource =
            systemAppearanceSource;
    }

    public ApplicationThemePreference CurrentPreference
    {
        get;
        private set;
    } =
        ApplicationThemePreference.Default;

    public ApplicationAppearance EffectiveAppearance
    {
        get;
        private set;
    } =
        ApplicationAppearance.Light;

    public async Task InitializeAsync(
        CancellationToken cancellationToken = default)
    {
        ApplicationThemePreference preference =
            await _preferenceStore
                .LoadAsync(
                    cancellationToken)
            ?? ApplicationThemePreference.Default;

        ApplyResources(
            preference);

        CurrentPreference =
            preference;
    }

    public async Task ApplyAsync(
        ApplicationThemePreference preference,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(
            preference);

        ValidatePreference(
            preference);

        ApplyResources(
            preference);

        await _preferenceStore
            .SaveAsync(
                preference,
                cancellationToken);

        CurrentPreference =
            preference;
    }

    private void ApplyResources(
        ApplicationThemePreference preference)
    {
        ValidatePreference(
            preference);

        ApplicationAppearance effectiveAppearance =
            preference.Appearance ==
                ApplicationAppearance.System
                ? _systemAppearanceSource
                    .GetCurrentAppearance()
                : preference.Appearance;

        if (effectiveAppearance is not
            ApplicationAppearance.Light
            and not ApplicationAppearance.Dark)
        {
            effectiveAppearance =
                ApplicationAppearance.Light;
        }

        string appearancePath =
            effectiveAppearance switch
            {
                ApplicationAppearance.Light =>
                    $"{AppearancePathPrefix}Light.xaml",

                ApplicationAppearance.Dark =>
                    $"{AppearancePathPrefix}Dark.xaml",

                _ =>
                    throw new InvalidOperationException(
                        "Appearance hiệu lực không hợp lệ.")
            };

        string accentPath =
            preference.Accent switch
            {
                ApplicationAccent.Blue =>
                    $"{AccentPathPrefix}Blue.xaml",

                ApplicationAccent.Green =>
                    $"{AccentPathPrefix}Green.xaml",

                _ =>
                    throw new InvalidOperationException(
                        "Accent không được hỗ trợ.")
            };

        ReplaceDictionary(
            "Themes/Appearance/",
            appearancePath,
            0);

        ReplaceDictionary(
            "Themes/Accents/",
            accentPath,
            1);

        EffectiveAppearance =
            effectiveAppearance;
    }

    private static void ReplaceDictionary(
        string marker,
        string sourcePath,
        int preferredIndex)
    {
        ResourceDictionary resources =
            System.Windows.Application.Current?.Resources
            ?? throw new InvalidOperationException(
                "Application resources chưa được khởi tạo.");

        ResourceDictionary? existing =
            resources.MergedDictionaries
                .FirstOrDefault(
                    dictionary =>
                        IsDictionary(
                            dictionary,
                            marker));

        int index =
            existing is not null
                ? resources.MergedDictionaries
                    .IndexOf(
                        existing)
                : Math.Min(
                    preferredIndex,
                    resources.MergedDictionaries.Count);

        if (existing is not null)
        {
            resources.MergedDictionaries.Remove(
                existing);
        }

        var replacement =
            new ResourceDictionary
            {
                Source =
                    new Uri(
                        sourcePath,
                        UriKind.RelativeOrAbsolute)
            };

        resources.MergedDictionaries.Insert(
            index,
            replacement);
    }

    private static bool IsDictionary(
        ResourceDictionary dictionary,
        string marker)
    {
        string? source =
            dictionary.Source?.OriginalString;

        if (string.IsNullOrWhiteSpace(
                source))
        {
            return false;
        }

        return source
            .Replace(
                '\\',
                '/')
            .Contains(
                marker,
                StringComparison.OrdinalIgnoreCase);
    }

    private static void ValidatePreference(
        ApplicationThemePreference preference)
    {
        if (!Enum.IsDefined(
                preference.Appearance))
        {
            throw new ArgumentOutOfRangeException(
                nameof(preference),
                "Appearance không hợp lệ.");
        }

        if (!Enum.IsDefined(
                preference.Accent))
        {
            throw new ArgumentOutOfRangeException(
                nameof(preference),
                "Accent không hợp lệ.");
        }
    }
}
