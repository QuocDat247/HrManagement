using HrManagement.Desktop.Theming;

namespace HrManagement.Desktop.ViewModels;

public sealed record SettingsAccentOption(
    ApplicationAccent Value,
    string DisplayName,
    string Description);
