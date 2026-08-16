namespace HrManagement.Infrastructure.Persistence;

public static class DatabasePath
{
    private const string ApplicationFolderName =
        "HrManagement";

    private const string DatabaseFileName =
        "hrmanagement.db";

    public static string GetDatabaseFilePath()
    {
        string localApplicationData =
            Environment.GetFolderPath(
                Environment.SpecialFolder.LocalApplicationData);

        string applicationDirectory =
            Path.Combine(
                localApplicationData,
                ApplicationFolderName);

        Directory.CreateDirectory(
            applicationDirectory);

        return Path.Combine(
            applicationDirectory,
            DatabaseFileName);
    }

    public static string GetConnectionString()
    {
        return $"Data Source={GetDatabaseFilePath()}";
    }
}
