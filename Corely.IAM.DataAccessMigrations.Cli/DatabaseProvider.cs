namespace Corely.IAM.DataAccessMigrations.Cli;

public enum DatabaseProvider
{
    MySql,
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

    extension(DatabaseProvider provider)
    {
        internal string PlaceholderConnectionString() =>
            provider switch
            {
                DatabaseProvider.MsSql => "Server=.;Database=CorelyIam;Trusted_Connection=True;",
                _ => "Server=localhost;Database=CorelyIam;Uid=root;Pwd=;",
            };
    }
}
