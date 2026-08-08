using HrManagement.Desktop.Theming;

namespace HrManagement.Tests.Theming;

public sealed class ThemeServiceRuntimeTests
{
    [Fact]
    public void ApplyTheme_Green_ChangesCurrentTheme()
    {
        System.Windows.Application? application =
            System.Windows.Application.Current;

        bool createdApplication = false;

        if (application is null)
        {
            application =
                new System.Windows.Application();

            createdApplication = true;
        }

        try
        {
            var service = new ThemeService();

            service.ApplyTheme(AppTheme.Green);

            Assert.Equal(
                AppTheme.Green,
                service.CurrentTheme);
        }
        finally
        {
            if (createdApplication)
            {
                application.Shutdown();
            }
        }
    }
}