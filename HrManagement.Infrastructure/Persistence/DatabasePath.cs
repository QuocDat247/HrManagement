namespace HrManagement.Infrastructure.Persistence;

public static class DatabasePath
{
    private const string ApplicationFolderName =
        "HrManagement";

    private const string ProductionDatabaseFileName =
        "hrmanagement.db";

    private const string DemoFolderName =
        "Demo";

    private const string DemoDatabaseFileName =
        "hrmanagement-demo.db";

    public static string GetDatabaseFilePath(
        ApplicationDataMode dataMode =
            ApplicationDataMode.Production)
    {
        string localApplicationData =
            Environment.GetFolderPath(
                Environment.SpecialFolder.LocalApplicationData);

        string applicationDirectory =
            Path.Combine(
                localApplicationData,
                ApplicationFolderName);

        if (dataMode ==
            ApplicationDataMode.Demo)
        {
            applicationDirectory =
                Path.Combine(
                    applicationDirectory,
                    DemoFolderName);
        }

        Directory.CreateDirectory(
            applicationDirectory);

        string databaseFileName =
            dataMode ==
                ApplicationDataMode.Demo
                ? DemoDatabaseFileName
                : ProductionDatabaseFileName;

        return Path.Combine(
            applicationDirectory,
            databaseFileName);
    }

    public static string GetConnectionString(
        ApplicationDataMode dataMode =
            ApplicationDataMode.Production)
    {
        return $"Data Source={GetDatabaseFilePath(dataMode)}";
    }
}
