namespace Corely.IAM.DataAccessMigrations.Cli;

public enum DatabaseProvider
{
    MySql,
    MariaDb,
    MsSql,
}

public static class DatabaseProviderExtensions
{
    public static bool TryParse(string? value, out DatabaseProvider provider)
    {
        if (Enum.TryParse(value, ignoreCase: true, out provider))
        {
            return true;
        }

        provider = default;
        return false;
    }

    public static string[] GetNames() => Enum.GetNames<DatabaseProvider>();
}
