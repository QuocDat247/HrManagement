using System.Reflection;

namespace HrManagement.Desktop.BuildIdentity;

public static class ApplicationBuildIdentityResolver
{
    private const string CustomerProfileMetadataKey =
        "HrCustomerProfile";

    private const string EditionMetadataKey =
        "HrProductEdition";

    private const string CustomerMetadataKey =
        "HrCustomerCode";

    private const string ReleaseChannelMetadataKey =
        "HrReleaseChannel";

    public static ApplicationBuildIdentity
        ResolveCurrent()
    {
        Assembly? entryAssembly =
            Assembly.GetEntryAssembly();

        if (entryAssembly is null)
        {
            return CreateUnknown();
        }

        return Resolve(
            entryAssembly);
    }

    public static ApplicationBuildIdentity Resolve(
        Assembly assembly)
    {
        ArgumentNullException.ThrowIfNull(
            assembly);

        string coreVersion =
            assembly
                .GetCustomAttribute<
                    AssemblyInformationalVersionAttribute>()
                ?.InformationalVersion
            ?? assembly
                .GetName()
                .Version
                ?.ToString()
            ?? "unknown";

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

        return new ApplicationBuildIdentity(
            CoreVersion:
                coreVersion,
            Edition:
                GetMetadata(
                    metadata,
                    EditionMetadataKey,
                    "Unknown"),
            CustomerCode:
                GetMetadata(
                    metadata,
                    CustomerMetadataKey,
                    "Unknown"),
            ReleaseChannel:
                GetMetadata(
                    metadata,
                    ReleaseChannelMetadataKey,
                    "Unknown"),
            CustomerProfile:
                GetMetadata(
                    metadata,
                    CustomerProfileMetadataKey,
                    "Unknown"));
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

    private static ApplicationBuildIdentity
        CreateUnknown()
    {
        return new ApplicationBuildIdentity(
            CoreVersion:
                "unknown",
            Edition:
                "Unknown",
            CustomerCode:
                "Unknown",
            ReleaseChannel:
                "Unknown",
            CustomerProfile:
                "Unknown");
    }
}
