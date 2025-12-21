using Corely.IAM.DataAccessMigrations.Attributes;

namespace Corely.IAM.DataAccessMigrations.Commands.ConfigCommands;

internal class Init() : CommandBase("init", "Create a new settings file")
{
    [Option("-f", "--force", Description = "Overwrite existing settings file")]
    private bool Force { get; init; }

    [Option("-c", "--connection", Description = "Database connection string")]
    private string ConnectionString { get; init; } = null!;

    protected override Task ExecuteAsync()
    {
        var settingsPath = Path.Combine(
            Directory.GetCurrentDirectory(),
            ConfigurationProvider.SettingsFileName
        );

        if (File.Exists(settingsPath) && !Force)
        {
            Error($"Settings file already exists: {settingsPath}");
            Info("Use --force to overwrite the existing file.");
            return Task.CompletedTask;
        }

        var connectionString = string.IsNullOrEmpty(ConnectionString)
            ? "Server=localhost;Database=YourDatabase;User=root;Password=yourpassword;"
            : ConnectionString;

        var settingsContent =
            $@"{{
  ""ConnectionStrings"": {{
    ""DataRepoConnection"": ""{connectionString}""
  }}
}}";

        File.WriteAllText(settingsPath, settingsContent);
        Success($"Settings file created: {settingsPath}");

        if (string.IsNullOrEmpty(ConnectionString))
        {
            Warn("Remember to update the connection string in the settings file.");
        }

        return Task.CompletedTask;
    }
}
