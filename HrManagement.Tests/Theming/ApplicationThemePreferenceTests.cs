using HrManagement.Desktop.Theming;

namespace HrManagement.Tests.Theming;

public sealed class ApplicationThemePreferenceTests
{
    [Fact]
    public void Default_UsesSystemAppearanceAndBlueAccent()
    {
        ApplicationThemePreference preference =
            ApplicationThemePreference.Default;

        Assert.Equal(
            ApplicationAppearance.System,
            preference.Appearance);

        Assert.Equal(
            ApplicationAccent.Blue,
            preference.Accent);
    }

    [Theory]
    [InlineData(ApplicationAppearance.System)]
    [InlineData(ApplicationAppearance.Light)]
    [InlineData(ApplicationAppearance.Dark)]
    public void ApplicationAppearance_SupportedValue_IsDefined(
        ApplicationAppearance appearance)
    {
        Assert.True(
            Enum.IsDefined(
                typeof(ApplicationAppearance),
                appearance));
    }

    [Theory]
    [InlineData(ApplicationAccent.Blue)]
    [InlineData(ApplicationAccent.Green)]
    public void ApplicationAccent_SupportedValue_IsDefined(
        ApplicationAccent accent)
    {
        Assert.True(
            Enum.IsDefined(
                typeof(ApplicationAccent),
                accent));
    }
}
