using Corely.IAM.DataAccess;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Corely.IAM.DataAccessMigrations.Cli.Commands.DatabaseCommands;

internal class Create(IServiceProvider serviceProvider)
    : CommandBase("create", "Create the database and apply all migrations")
{
    protected override async Task ExecuteAsync()
    {
        if (!ValidateSettings(out _))
            return;

        try
        {
            using var dbContext = serviceProvider.GetRequiredService<IamDbContext>();

            Info("Creating database and applying migrations...");
            await dbContext.Database.MigrateAsync();
            Success("Database created and migrations applied successfully.");
        }
        catch (Exception ex)
        {
            Error($"Failed to create database: {ex.Message}");
        }
    }
}
