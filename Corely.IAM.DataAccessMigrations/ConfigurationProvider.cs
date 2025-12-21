using Microsoft.Extensions.Configuration;

namespace Corely.IAM.DataAccessMigrations;

internal static class ConfigurationProvider
{
    private const string SETTINGS_FILE_NAME = "corely-iam-db-migration-settings.json";

    private static readonly IConfigurationRoot _configuration;

    static ConfigurationProvider()
    {
        var builder = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile(SETTINGS_FILE_NAME, optional: true, reloadOnChange: true);

        _configuration = builder.Build();
    }

    public static string? TryGetConnectionString()
    {
        return _configuration.GetConnectionString("DataRepoConnection");
    }

    public static string GetConnectionString()
    {
        return TryGetConnectionString()
            ?? throw new InvalidOperationException(
                $"DataRepoConnection string not found in {SETTINGS_FILE_NAME}. "
                    + $"Run 'config init' to create a settings file or 'config path' to see the expected location."
            );
    }

    public static bool HasConnectionString => TryGetConnectionString() != null;

    public static string SettingsFileName => SETTINGS_FILE_NAME;
}
