using System.Reflection;

namespace HrManagement.Desktop.Branding;

public static class ApplicationBrandingResolver
{
    private const string ProductDisplayNameKey =
        "HrBrandProductDisplayName";

    private const string CustomerDisplayNameKey =
        "HrBrandCustomerDisplayName";

    private const string SupportLabelKey =
        "HrBrandSupportLabel";

    private const string SupportContactKey =
        "HrBrandSupportContact";

    public static ApplicationBranding ResolveCurrent()
    {
        Assembly? entryAssembly =
            Assembly.GetEntryAssembly();

        return entryAssembly is null
            ? ApplicationBranding.Default
            : Resolve(
                entryAssembly);
    }

    public static ApplicationBranding Resolve(
        Assembly assembly)
    {
        ArgumentNullException.ThrowIfNull(
            assembly);

        Dictionary<string, string>
            metadata =
                assembly
                    .GetCustomAttributes<
                        AssemblyMetadataAttribute>()
                    .Where(
                        attribute =>
                            !string.IsNullOrWhiteSpace(
                                attribute.Key)
                            && !string.IsNullOrWhiteSpace(
                                attribute.Value))
                    .GroupBy(
                        attribute =>
                            attribute.Key,
                        StringComparer.Ordinal)
                    .ToDictionary(
                        group =>
                            group.Key,
                        group =>
                            group.Last().Value!,
                        StringComparer.Ordinal);

        ApplicationBranding defaults =
            ApplicationBranding.Default;

        return new ApplicationBranding(
            ProductDisplayName:
                GetMetadata(
                    metadata,
                    ProductDisplayNameKey,
                    defaults.ProductDisplayName),

            CustomerDisplayName:
                GetMetadata(
                    metadata,
                    CustomerDisplayNameKey,
                    defaults.CustomerDisplayName),

            SupportLabel:
                GetMetadata(
                    metadata,
                    SupportLabelKey,
                    defaults.SupportLabel),

            SupportContact:
                GetMetadata(
                    metadata,
                    SupportContactKey,
                    defaults.SupportContact));
    }

    private static string GetMetadata(
        IReadOnlyDictionary<string, string> metadata,
        string key,
        string fallback)
    {
        return metadata.TryGetValue(
                key,
                out string? value)
            && !string.IsNullOrWhiteSpace(
                value)
                ? value
                : fallback;
    }
}
