namespace Corely.IAM.DataAccessMigrations.Commands.ConfigCommands;

internal class Show()
    : CommandBase("show", "Display the current settings file location and contents")
{
    protected override Task ExecuteAsync()
    {
        var settingsPath = ConfigurationProvider.SettingsFilePath;

        Info($"Settings file location: {settingsPath}");
        Info("");

        if (!File.Exists(settingsPath))
        {
            Warn("Settings file does not exist.");
            Info("Run 'config init' to create a new settings file.");
            return Task.CompletedTask;
        }

        try
        {
            var content = File.ReadAllText(settingsPath);
            Info("=== Settings File Contents ===");
            Info(content);
        }
        catch (Exception ex)
        {
            Error($"Failed to read settings file: {ex.Message}");
        }

        return Task.CompletedTask;
    }
}
