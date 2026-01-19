using Corely.IAM.DataAccessMigrations.Cli.Attributes;

namespace Corely.IAM.DataAccessMigrations.Cli.Commands.ConfigCommands;

internal class Init()
    : CommandBase("init", "Create a new settings file with the specified provider")
{
    [Argument("The database provider to use (MySql, MariaDb)")]
    private string ProviderName { get; init; } = null!;

    [Option("-f", "--force", Description = "Overwrite existing settings file")]
    private bool Force { get; init; }

    [Option("-c", "--connection", Description = "Database connection string")]
    private string ConnectionString { get; init; } = null!;

    protected override Task ExecuteAsync()
    {
        if (string.IsNullOrEmpty(ProviderName))
        {
            Error("Provider name is required.");
            Info(
                $"Available providers: {string.Join(", ", DatabaseProviderExtensions.GetNames())}"
            );
            Info("Usage: config init <provider> [-f] [-c <connection>]");
            return Task.CompletedTask;
        }

        if (!DatabaseProviderExtensions.TryParse(ProviderName, out var provider))
        {
            Error($"Invalid provider: {ProviderName}");
            Info(
                $"Available providers: {string.Join(", ", DatabaseProviderExtensions.GetNames())}"
            );
            return Task.CompletedTask;
        }

        var settingsPath = ConfigurationProvider.SettingsFilePath;

        if (File.Exists(settingsPath) && !Force)
        {
            Error($"Settings file already exists: {settingsPath}");
            Info("Use --force to overwrite the existing file.");
            return Task.CompletedTask;
        }

        var connectionString = string.IsNullOrEmpty(ConnectionString)
            ? "Server=localhost;Port=3306;Database=YourDatabase;Uid=root;Pwd=yourpassword;"
            : ConnectionString;

        var settingsContent = $$"""
            {
              "Provider": "{{provider}}",
              "ConnectionStrings": {
                "DataRepoConnection": "{{connectionString}}"
              }
            }
            """;

        File.WriteAllText(settingsPath, settingsContent);
        Success($"Settings file created: {settingsPath}");
        Info($"Provider: {provider}");

        if (string.IsNullOrEmpty(ConnectionString))
        {
            Warn("Remember to update the connection string in the settings file.");
        }

        return Task.CompletedTask;
    }
}
