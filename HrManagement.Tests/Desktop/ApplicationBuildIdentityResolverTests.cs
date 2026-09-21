using System.Reflection;
using HrManagement.Desktop.BuildIdentity;

namespace HrManagement.Tests.Desktop;

public sealed class ApplicationBuildIdentityResolverTests
{
    [Fact]
    public void Resolve_UsesDesktopAssemblyMetadata()
    {
        Assembly desktopAssembly =
            typeof(HrManagement.Desktop.App)
                .Assembly;

        string? expectedVersion =
            desktopAssembly
                .GetCustomAttribute<
                    AssemblyInformationalVersionAttribute>()
                ?.InformationalVersion;

        ApplicationBuildIdentity identity =
            ApplicationBuildIdentityResolver.Resolve(
                desktopAssembly);

        Assert.False(
            string.IsNullOrWhiteSpace(
                expectedVersion));

        Assert.Equal(
            expectedVersion,
            identity.CoreVersion);

        Assert.Equal(
            "Standard",
            identity.Edition);

        Assert.Equal(
            "Generic",
            identity.CustomerCode);

        Assert.Equal(
            "Development",
            identity.ReleaseChannel);
    }
}
