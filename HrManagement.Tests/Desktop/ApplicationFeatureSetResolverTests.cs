using System.Reflection;
using HrManagement.Desktop.Features;

namespace HrManagement.Tests.Desktop;

public sealed class
    ApplicationFeatureSetResolverTests
{
    [Fact]
    public void Resolve_DefaultDesktopBuild_HasAllFeaturesEnabled()
    {
        Assembly desktopAssembly =
            typeof(HrManagement.Desktop.App)
                .Assembly;

        ApplicationFeatureSet featureSet =
            ApplicationFeatureSetResolver.Resolve(
                desktopAssembly);

        Assert.True(
            featureSet.Employees);

        Assert.True(
            featureSet.Organization);

        Assert.True(
            featureSet.TimeManagement);

        Assert.True(
            featureSet.Payroll);
    }
}
