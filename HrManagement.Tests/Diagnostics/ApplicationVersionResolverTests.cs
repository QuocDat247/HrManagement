using System.Reflection;
using HrManagement.Desktop.Diagnostics;

namespace HrManagement.Tests.Diagnostics;

public sealed class ApplicationVersionResolverTests
{
    [Fact]
    public void GetCurrentVersion_PrefersInformationalVersion()
    {
        Assembly entryAssembly =
            Assert.IsAssignableFrom<Assembly>(
                Assembly.GetEntryAssembly());

        string? expected =
            entryAssembly
                .GetCustomAttribute<
                    AssemblyInformationalVersionAttribute>()
                ?.InformationalVersion;

        Assert.False(
            string.IsNullOrWhiteSpace(
                expected));

        string actual =
            ApplicationVersionResolver
                .GetCurrentVersion();

        Assert.Equal(
            expected,
            actual);
    }
}
