using System.Reflection;
using HrManagement.Desktop.Branding;

namespace HrManagement.Tests.Desktop;

public sealed class ApplicationBrandingResolverTests
{
    [Fact]
    public void Resolve_UsesDesktopAssemblyMetadata()
    {
        Assembly desktopAssembly =
            typeof(HrManagement.Desktop.App)
                .Assembly;

        ApplicationBranding branding =
            ApplicationBrandingResolver.Resolve(
                desktopAssembly);

        Assert.Equal(
            "HR Management",
            branding.ProductDisplayName);

        Assert.Equal(
            "Bản tiêu chuẩn",
            branding.CustomerDisplayName);

        Assert.Equal(
            "Hỗ trợ kỹ thuật",
            branding.SupportLabel);

        Assert.Equal(
            "Liên hệ nhà cung cấp triển khai",
            branding.SupportContact);
    }
}
