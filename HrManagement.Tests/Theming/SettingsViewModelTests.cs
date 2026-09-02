using HrManagement.Desktop.Theming;
using HrManagement.Desktop.ViewModels;

namespace HrManagement.Tests.Theming;

public sealed class SettingsViewModelTests
{
    [Fact]
    public void Constructor_LoadsCurrentThemePreference()
    {
        var themeService =
            new StubApplicationThemeService
            {
                CurrentPreferenceValue =
                    new ApplicationThemePreference(
                        ApplicationAppearance.Dark,
                        ApplicationAccent.Green),

                EffectiveAppearanceValue =
                    ApplicationAppearance.Dark
            };

        var viewModel =
            new SettingsViewModel(
                themeService);

        Assert.Equal(
            ApplicationAppearance.Dark,
            viewModel.SelectedAppearance);

        Assert.Equal(
            ApplicationAccent.Green,
            viewModel.SelectedAccent);

        Assert.Equal(
            3,
            viewModel.AppearanceOptions.Count);

        Assert.Equal(
            2,
            viewModel.AccentOptions.Count);

        Assert.False(
            viewModel.HasChanges);

        Assert.False(
            viewModel.CanApplyChanges);
    }

    [Fact]
    public async Task ApplyAsync_WhenSelectionChanges_AppliesAndPersistsPreference()
    {
        var themeService =
            new StubApplicationThemeService
            {
                CurrentPreferenceValue =
                    new ApplicationThemePreference(
                        ApplicationAppearance.Light,
                        ApplicationAccent.Blue),

                EffectiveAppearanceValue =
                    ApplicationAppearance.Light
            };

        var viewModel =
            new SettingsViewModel(
                themeService);

        viewModel.SelectedAppearance =
            ApplicationAppearance.Dark;

        viewModel.SelectedAccent =
            ApplicationAccent.Green;

        Assert.True(
            viewModel.HasChanges);

        await viewModel.ApplyCommand.ExecuteAsync(
            null);

        Assert.Equal(
            1,
            themeService.ApplyCallCount);

        Assert.Equal(
            ApplicationAppearance.Dark,
            themeService.CurrentPreference.Appearance);

        Assert.Equal(
            ApplicationAccent.Green,
            themeService.CurrentPreference.Accent);

        Assert.False(
            viewModel.HasChanges);

        Assert.False(
            viewModel.CanApplyChanges);

        Assert.Equal(
            "Đã áp dụng và lưu cài đặt giao diện.",
            viewModel.SuccessMessage);
    }

    [Fact]
    public void LoadCommand_RestoresServicePreference()
    {
        var themeService =
            new StubApplicationThemeService
            {
                CurrentPreferenceValue =
                    new ApplicationThemePreference(
                        ApplicationAppearance.Light,
                        ApplicationAccent.Blue),

                EffectiveAppearanceValue =
                    ApplicationAppearance.Light
            };

        var viewModel =
            new SettingsViewModel(
                themeService);

        viewModel.SelectedAppearance =
            ApplicationAppearance.Dark;

        Assert.True(
            viewModel.HasChanges);

        viewModel.LoadCommand.Execute(
            null);

        Assert.Equal(
            ApplicationAppearance.Light,
            viewModel.SelectedAppearance);

        Assert.Equal(
            ApplicationAccent.Blue,
            viewModel.SelectedAccent);

        Assert.False(
            viewModel.HasChanges);
    }

    private sealed class StubApplicationThemeService
        : IApplicationThemeService
    {
        public ApplicationThemePreference
            CurrentPreferenceValue
        {
            get;
            set;
        } =
            ApplicationThemePreference.Default;

        public ApplicationAppearance
            EffectiveAppearanceValue
        {
            get;
            set;
        } =
            ApplicationAppearance.Light;

        public int ApplyCallCount
        {
            get;
            private set;
        }

        public ApplicationThemePreference
            CurrentPreference =>
                CurrentPreferenceValue;

        public ApplicationAppearance
            EffectiveAppearance =>
                EffectiveAppearanceValue;

        public Task InitializeAsync(
            CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public Task ApplyAsync(
            ApplicationThemePreference preference,
            CancellationToken cancellationToken = default)
        {
            ApplyCallCount++;

            CurrentPreferenceValue =
                preference;

            EffectiveAppearanceValue =
                preference.Appearance ==
                    ApplicationAppearance.System
                    ? ApplicationAppearance.Light
                    : preference.Appearance;

            return Task.CompletedTask;
        }
    }
}
