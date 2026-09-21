using System.Reflection;

namespace HrManagement.Desktop.Features;

public static class ApplicationFeatureSetResolver
{
    private const string EmployeesKey =
        "HrFeatureEmployees";

    private const string OrganizationKey =
        "HrFeatureOrganization";

    private const string TimeManagementKey =
        "HrFeatureTimeManagement";

    private const string PayrollKey =
        "HrFeaturePayroll";

    public static ApplicationFeatureSet
        ResolveCurrent()
    {
        Assembly? entryAssembly =
            Assembly.GetEntryAssembly();

        if (entryAssembly is null)
        {
            return ApplicationFeatureSet.AllEnabled;
        }

        return Resolve(
            entryAssembly);
    }

    public static ApplicationFeatureSet Resolve(
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

        return new ApplicationFeatureSet(
            Employees:
                GetBoolean(
                    metadata,
                    EmployeesKey,
                    defaultValue: true),

            Organization:
                GetBoolean(
                    metadata,
                    OrganizationKey,
                    defaultValue: true),

            TimeManagement:
                GetBoolean(
                    metadata,
                    TimeManagementKey,
                    defaultValue: true),

            Payroll:
                GetBoolean(
                    metadata,
                    PayrollKey,
                    defaultValue: true));
    }

    private static bool GetBoolean(
        IReadOnlyDictionary<string, string> metadata,
        string key,
        bool defaultValue)
    {
        if (!metadata.TryGetValue(
                key,
                out string? value))
        {
            return defaultValue;
        }

        if (bool.TryParse(
                value,
                out bool result))
        {
            return result;
        }

        throw new InvalidOperationException(
            $"Build metadata '{key}' "
            + $"contains invalid boolean value '{value}'.");
    }
}
