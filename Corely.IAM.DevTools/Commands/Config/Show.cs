namespace Corely.IAM.DevTools.Commands.Config;

internal partial class Config
{
    internal class Show : CommandBase
    {
        public Show()
            : base("show", "Display the current settings file location and contents") { }

        protected override void Execute()
        {
            var settingsPath = ConfigurationProvider.SettingsFilePath;

            Info($"Settings file location: {settingsPath}");
            Info("");

            if (!File.Exists(settingsPath))
            {
                Warn("Settings file does not exist.");
                Info("Run 'config init' to create a new settings file.");
                return;
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
        }
    }
}
