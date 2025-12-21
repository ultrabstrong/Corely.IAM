using System.Text.Json;
using Corely.IAM.DevTools.Attributes;
using Corely.Security.Encryption;
using Corely.Security.Encryption.Factories;

namespace Corely.IAM.DevTools.Commands.Config;

internal partial class Config
{
    internal class SystemKey : CommandBase
    {
        private readonly SymmetricEncryptionProviderFactory _encryptionProviderFactory = new(
            SymmetricEncryptionConstants.AES_CODE
        );

        [Option(
            "-g",
            "--generate",
            Description = "Generate a new random encryption key and save it to settings"
        )]
        private bool Generate { get; init; }

        [Option("-s", "--show", Description = "Show the current encryption key from settings")]
        private bool Show { get; init; }

        [Option("-k", "--key", Description = "Set a specific encryption key")]
        private string Key { get; init; } = null!;

        public SystemKey()
            : base("system-key", "Manage the SystemSymmetricEncryptionKey in the settings file") { }

        protected override void Execute()
        {
            if (!Generate && !Show && string.IsNullOrWhiteSpace(Key))
            {
                Error("Please specify an action: --generate, --show, or --key <value>");
                return;
            }

            if (Show)
            {
                ShowCurrentKey();
                return;
            }

            if (Generate || !string.IsNullOrWhiteSpace(Key))
            {
                SetKey();
            }
        }

        private void ShowCurrentKey()
        {
            var key = ConfigurationProvider.TryGetSystemSymmetricEncryptionKey();

            if (string.IsNullOrEmpty(key))
            {
                Warn("SystemSymmetricEncryptionKey is not set.");
                Info("Run 'config system-key --generate' to create one.");
            }
            else
            {
                Info($"SystemSymmetricEncryptionKey: {key}");
            }
        }

        private void SetKey()
        {
            var settingsPath = ConfigurationProvider.SettingsFilePath;

            if (!File.Exists(settingsPath))
            {
                Error($"Settings file not found: {settingsPath}");
                Info("Run 'config init' to create a new settings file first.");
                return;
            }

            string keyToSet;
            if (Generate)
            {
                var encryptionProvider = _encryptionProviderFactory.GetProvider(
                    SymmetricEncryptionConstants.AES_CODE
                );
                keyToSet = encryptionProvider.GetSymmetricKeyProvider().CreateKey();
            }
            else
            {
                keyToSet = Key;
            }

            try
            {
                var json = File.ReadAllText(settingsPath);
                using var doc = JsonDocument.Parse(json);

                var options = new JsonWriterOptions { Indented = true };
                using var stream = new MemoryStream();
                using (var writer = new Utf8JsonWriter(stream, options))
                {
                    writer.WriteStartObject();

                    foreach (var property in doc.RootElement.EnumerateObject())
                    {
                        if (property.Name == "SystemSymmetricEncryptionKey")
                        {
                            writer.WriteString("SystemSymmetricEncryptionKey", keyToSet);
                        }
                        else
                        {
                            property.WriteTo(writer);
                        }
                    }

                    writer.WriteEndObject();
                }

                var updatedJson = System.Text.Encoding.UTF8.GetString(stream.ToArray());
                File.WriteAllText(settingsPath, updatedJson);

                Success("SystemSymmetricEncryptionKey updated successfully.");
                Info($"Key: {keyToSet}");

                if (Generate)
                {
                    Warn("Make sure to save this key securely - it cannot be recovered!");
                }
            }
            catch (JsonException ex)
            {
                Error($"Failed to parse settings file: {ex.Message}");
            }
            catch (Exception ex)
            {
                Error($"Failed to update settings file: {ex.Message}");
            }
        }
    }
}
