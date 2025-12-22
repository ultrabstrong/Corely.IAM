using Corely.IAM.DevTools.Attributes;

namespace Corely.IAM.DevTools.Commands.Config;

internal partial class Config
{
    internal class Init : CommandBase
    {
        [Option("-f", "--force", Description = "Overwrite existing settings file")]
        private bool Force { get; init; }

        [Option("-c", "--connection", Description = "Database connection string")]
        private string ConnectionString { get; init; } = null!;

        public Init()
            : base("init", "Create a new settings file") { }

        protected override void Execute()
        {
            var settingsPath = ConfigurationProvider.SettingsFilePath;

            if (File.Exists(settingsPath) && !Force)
            {
                Error($"Settings file already exists: {settingsPath}");
                Info("Use --force to overwrite the existing file.");
                return;
            }

            var connectionString = string.IsNullOrEmpty(ConnectionString)
                ? "Server=localhost;Port=3306;Database=YourDatabase;Uid=root;Pwd=yourpassword;"
                : ConnectionString;

            var settingsContent =
                $@"{{
  ""ConnectionStrings"": {{
    ""DataRepoConnection"": ""{connectionString}""
  }},
  ""SystemSymmetricEncryptionKey"": """",
  ""PasswordValidationOptions"": {{
    ""MinimumLength"": 8,
    ""RequireUppercase"": true,
    ""RequireLowercase"": true,
    ""RequireDigit"": true,
    ""RequireNonAlphanumeric"": true
  }}
}}";

            File.WriteAllText(settingsPath, settingsContent);
            Success($"Settings file created: {settingsPath}");

            if (string.IsNullOrEmpty(ConnectionString))
            {
                Warn("Remember to update the configuration values in the settings file.");
                Warn("Run 'config system-key --generate' to create an encryption key.");
            }
        }
    }
}
