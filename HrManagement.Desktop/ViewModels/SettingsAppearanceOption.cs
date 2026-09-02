using HrManagement.Desktop.Theming;

namespace HrManagement.Desktop.ViewModels;

public sealed record SettingsAppearanceOption(
    ApplicationAppearance Value,
    string DisplayName,
    string Description);
