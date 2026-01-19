namespace Corely.IAM.DataAccessMigrations.Cli.Commands.ProviderCommands;

internal class Show() : CommandBase("show", "Show current database provider from config")
{
    protected override Task ExecuteAsync()
    {
        var settingsPath = ConfigurationProvider.SettingsFilePath;

        if (!File.Exists(settingsPath))
        {
            Warn("Settings file does not exist.");
            Info("Run 'config init <provider>' to create a new settings file.");
            return Task.CompletedTask;
        }

        var provider = ConfigurationProvider.TryGetProvider();

        if (provider is null)
        {
            Warn("Provider not set or invalid in settings file.");
            Info("Run 'provider set <provider>' to set the provider.");
        }
        else
        {
            Info($"Current provider: {provider}");
        }

        return Task.CompletedTask;
    }
}
