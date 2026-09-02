namespace HrManagement.Desktop.Theming;

public sealed record ApplicationThemePreference(
    ApplicationAppearance Appearance,
    ApplicationAccent Accent)
{
    public static ApplicationThemePreference Default =>
        new(
            ApplicationAppearance.System,
            ApplicationAccent.Blue);
}
