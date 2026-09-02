namespace HrManagement.Desktop.Theming;

public interface IApplicationThemePreferenceStore
{
    Task<ApplicationThemePreference?> LoadAsync(
        CancellationToken cancellationToken = default);

    Task SaveAsync(
        ApplicationThemePreference preference,
        CancellationToken cancellationToken = default);
}
