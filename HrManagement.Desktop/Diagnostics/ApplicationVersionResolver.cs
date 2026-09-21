using System.Reflection;

namespace HrManagement.Desktop.Diagnostics;

public static class ApplicationVersionResolver
{
    public static string GetCurrentVersion()
    {
        Assembly? entryAssembly =
            Assembly.GetEntryAssembly();

        if (entryAssembly is null)
        {
            return "unknown";
        }

        string? informationalVersion =
            entryAssembly
                .GetCustomAttribute<
                    AssemblyInformationalVersionAttribute>()
                ?.InformationalVersion;

        if (!string.IsNullOrWhiteSpace(
                informationalVersion))
        {
            return informationalVersion;
        }

        return entryAssembly
                .GetName()
                .Version
                ?.ToString()
            ?? "unknown";
    }
}
