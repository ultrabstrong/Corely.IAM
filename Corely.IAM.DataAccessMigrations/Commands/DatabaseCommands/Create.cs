using Corely.IAM.DataAccess;
using Microsoft.Extensions.DependencyInjection;

namespace Corely.IAM.DataAccessMigrations.Commands.DatabaseCommands;

internal class Create(IServiceProvider serviceProvider)
    : CommandBase("create", "Create the database if it doesn't exist (without migrations)")
{
    protected override async Task ExecuteAsync()
    {
        if (!ValidateSettings(out _))
            return;

        try
        {
            using var dbContext = serviceProvider.GetRequiredService<IamDbContext>();

            Info("Creating database if it doesn't exist...");
            var created = await dbContext.Database.EnsureCreatedAsync();

            if (created)
            {
                Success("Database created successfully.");
                Warn(
                    "Note: EnsureCreated does not use migrations. Use 'migrate' command for production databases."
                );
            }
            else
            {
                Info("Database already exists.");
            }
        }
        catch (Exception ex)
        {
            Error($"Failed to create database: {ex.Message}");
        }
    }
}
