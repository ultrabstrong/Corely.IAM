using System.Text.Json;
using System.Text.Json.Nodes;
using Corely.IAM.DevTools.Attributes;

namespace Corely.IAM.DevTools.Commands.Provider;

internal partial class Provider
{
    internal class Set : CommandBase
    {
        [Argument("The database provider to use (MySql, MariaDb, MsSql)")]
        private string ProviderName { get; init; } = null!;

        public Set()
            : base("set", "Change the database provider in the config file") { }

        protected override void Execute()
        {
            if (string.IsNullOrEmpty(ProviderName))
            {
                Error("Provider name is required.");
                Info(
                    $"Available providers: {string.Join(", ", DatabaseProviderExtensions.GetNames())}"
                );
                return;
            }

            if (!DatabaseProviderExtensions.TryParse(ProviderName, out var provider))
            {
                Error($"Invalid provider: {ProviderName}");
                Info(
                    $"Available providers: {string.Join(", ", DatabaseProviderExtensions.GetNames())}"
                );
                return;
            }

            var settingsPath = ConfigurationProvider.SettingsFilePath;

            if (!File.Exists(settingsPath))
            {
                Error("Settings file does not exist.");
                Info("Run 'config init' to create a new settings file.");
                return;
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
        }
    }
}
