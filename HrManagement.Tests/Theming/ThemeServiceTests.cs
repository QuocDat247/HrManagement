using HrManagement.Desktop.Theming;

namespace HrManagement.Tests.Theming;

public sealed class ThemeServiceTests
{
    [Fact]
    public void NewThemeService_DefaultTheme_IsBlue()
    {
        var service = new ThemeService();

        Assert.Equal(
            AppTheme.Blue,
            service.CurrentTheme);
    }

    [Theory]
    [InlineData(AppTheme.Blue)]
    [InlineData(AppTheme.Green)]
    public void AppTheme_SupportedTheme_IsDefined(
        AppTheme theme)
    {
        Assert.True(
            Enum.IsDefined(typeof(AppTheme), theme));
    }
}