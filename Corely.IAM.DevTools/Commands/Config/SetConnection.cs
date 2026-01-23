using System.Text.Json;
using System.Text.Json.Nodes;
using Corely.IAM.DevTools.Attributes;

namespace Corely.IAM.DevTools.Commands.Config;

internal partial class Config
{
    internal class SetConnection()
        : CommandBase("set-connection", "Set the database connection string")
    {
        [Argument("The database connection string")]
        private string ConnectionString { get; init; } = null!;

        protected override Task ExecuteAsync()
        {
            if (string.IsNullOrEmpty(ConnectionString))
            {
                Error("Connection string is required.");
                Info(
                    "Usage: config set-connection \"Server=localhost;Port=3306;Database=mydb;...\""
                );
                return Task.CompletedTask;
            }

            var settingsPath = ConfigurationProvider.SettingsFilePath;

            if (!File.Exists(settingsPath))
            {
                Error("Settings file does not exist.");
                Info("Run 'config init' to create a new settings file.");
                return Task.CompletedTask;
            }

            try
            {
                var json = File.ReadAllText(settingsPath);
                var jsonNode = JsonNode.Parse(json) ?? new JsonObject();

                var connectionStrings =
                    jsonNode["ConnectionStrings"]?.AsObject() ?? new JsonObject();
                connectionStrings["DataRepoConnection"] = ConnectionString;
                jsonNode["ConnectionStrings"] = connectionStrings;

                var options = new JsonSerializerOptions { WriteIndented = true };
                File.WriteAllText(settingsPath, jsonNode.ToJsonString(options));

                Success("Connection string updated.");
            }
            catch (Exception ex)
            {
                Error($"Failed to update settings file: {ex.Message}");
            }

            return Task.CompletedTask;
        }
    }
}
