using System.Text.Json;
using System.Text.Json.Nodes;
using Corely.IAM.DataAccessMigrations.Cli.Attributes;

namespace Corely.IAM.DataAccessMigrations.Cli.Commands.ProviderCommands;

internal class Set() : CommandBase("set", "Change the database provider in the config file")
{
    [Argument("The database provider to use (MySql, MariaDb)")]
    private string ProviderName { get; init; } = null!;

    protected override Task ExecuteAsync()
    {
        if (string.IsNullOrEmpty(ProviderName))
        {
            Error("Provider name is required.");
            Info(
                $"Available providers: {string.Join(", ", DatabaseProviderExtensions.GetNames())}"
            );
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

        if (!File.Exists(settingsPath))
        {
            Error("Settings file does not exist.");
            Info("Run 'config init <provider>' to create a new settings file.");
            return Task.CompletedTask;
        }

        try
        {
            var json = File.ReadAllText(settingsPath);
            var jsonNode = JsonNode.Parse(json) ?? new JsonObject();

            jsonNode["Provider"] = provider.ToString();

            var options = new JsonSerializerOptions { WriteIndented = true };
            File.WriteAllText(settingsPath, jsonNode.ToJsonString(options));

            Success($"Provider changed to: {provider}");
        }
        catch (Exception ex)
        {
            Error($"Failed to update settings file: {ex.Message}");
        }

        return Task.CompletedTask;
    }
}
