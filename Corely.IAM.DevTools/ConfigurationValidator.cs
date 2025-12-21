namespace Corely.IAM.DevTools;

internal static class ConfigurationValidator
{
    public record ValidationResult(
        bool IsValid,
        string? ErrorMessage = null,
        string? Guidance = null
    );

    public static ValidationResult ValidateSettingsFile()
    {
        var settingsPath = ConfigurationProvider.SettingsFilePath;

        if (!File.Exists(settingsPath))
        {
            return new ValidationResult(
                false,
                $"Settings file not found: {settingsPath}",
                "Run 'config init' to create a new settings file, or 'config path' to see the expected location."
            );
        }

        return new ValidationResult(true);
    }

    public static ValidationResult ValidateFullConfiguration()
    {
        var settingsResult = ValidateSettingsFile();
        if (!settingsResult.IsValid)
        {
            return settingsResult;
        }

        if (!ConfigurationProvider.HasConnectionString)
        {
            return new ValidationResult(
                false,
                $"Connection string 'DataRepoConnection' not found in {ConfigurationProvider.SETTINGS_FILE_NAME}",
                "Run 'config show' to view the current settings file contents."
            );
        }

        if (!ConfigurationProvider.HasSystemSymmetricEncryptionKey)
        {
            return new ValidationResult(
                false,
                $"SystemSymmetricEncryptionKey not found in {ConfigurationProvider.SETTINGS_FILE_NAME}",
                "Run 'config system-key --generate' to create a new encryption key."
            );
        }

        return new ValidationResult(true);
    }
}
