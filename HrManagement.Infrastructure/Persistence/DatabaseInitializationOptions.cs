namespace HrManagement.Infrastructure.Persistence;

public sealed class DatabaseInitializationOptions
{
    public DatabaseInitializationOptions(
        ApplicationDataMode dataMode)
    {
        DataMode =
            dataMode;
    }

    public ApplicationDataMode DataMode
    {
        get;
    }
}
