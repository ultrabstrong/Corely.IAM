namespace Corely.IAM.DataAccessMigrations.Commands.ConfigCommands;

internal class ShowPath() : CommandBase("path", "Display the expected settings file path")
{
    protected override Task ExecuteAsync()
    {
        var settingsPath = ConfigurationProvider.SettingsFilePath;
        var exists = File.Exists(settingsPath);

        Info($"Settings file path: {settingsPath}");

        if (exists)
        {
            Success("File exists.");
        }
        else
        {
            Warn("File does not exist. Run 'config init' to create it.");
        }

        return Task.CompletedTask;
    }
}
