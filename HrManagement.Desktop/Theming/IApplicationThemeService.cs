namespace HrManagement.Desktop.Theming;

public interface IApplicationThemeService
{
    ApplicationThemePreference CurrentPreference
    {
        get;
    }

    ApplicationAppearance EffectiveAppearance
    {
        get;
    }

    Task InitializeAsync(
        CancellationToken cancellationToken = default);

    Task ApplyAsync(
        ApplicationThemePreference preference,
        CancellationToken cancellationToken = default);
}
