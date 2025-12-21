namespace Corely.IAM.DevTools.Commands.Config;

internal partial class Config
{
    internal class ShowPath : CommandBase
    {
        public ShowPath()
            : base("path", "Display the expected settings file path") { }

        protected override void Execute()
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
        }
    }
}
