using Corely.IAM.DataAccess;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Corely.IAM.DataAccessMigrations;

internal static class DatabaseConnectionValidator
{
    public record ValidationResult(
        bool IsValid,
        string? ErrorMessage = null,
        string? Guidance = null
    );

    public static ValidationResult ValidateSettingsFile()
    {
        var settingsPath = Path.Combine(
            Directory.GetCurrentDirectory(),
            ConfigurationProvider.SettingsFileName
        );

        if (!File.Exists(settingsPath))
        {
            return new ValidationResult(
                false,
                $"Settings file not found: {settingsPath}",
                "Run 'config init' to create a new settings file, or 'config path' to see the expected location."
            );
        }

        if (!ConfigurationProvider.HasConnectionString)
        {
            return new ValidationResult(
                false,
                $"Connection string 'DataRepoConnection' not found in {ConfigurationProvider.SettingsFileName}",
                "Run 'config show' to view the current settings file contents and ensure it contains a valid connection string."
            );
        }

        return new ValidationResult(true);
    }

    public static async Task<ValidationResult> ValidateConnectionAsync(
        IServiceProvider serviceProvider
    )
    {
        var settingsResult = ValidateSettingsFile();
        if (!settingsResult.IsValid)
        {
            return settingsResult;
        }

        try
        {
            using var dbContext = serviceProvider.GetRequiredService<IamDbContext>();
            var canConnect = await dbContext.Database.CanConnectAsync();

            if (!canConnect)
            {
                return new ValidationResult(
                    false,
                    "Could not connect to the database.",
                    "Verify the connection string in your settings file is correct. Run 'config show' to view current settings."
                );
            }

            return new ValidationResult(true);
        }
        catch (Exception ex)
        {
            return new ValidationResult(
                false,
                $"Database connection failed: {ex.Message}",
                "Check that the database server is running and the connection string is correct. Run 'config show' to view current settings."
            );
        }
    }

    public static ValidationResult ValidateSettings()
    {
        return ValidateSettingsFile();
    }
}
