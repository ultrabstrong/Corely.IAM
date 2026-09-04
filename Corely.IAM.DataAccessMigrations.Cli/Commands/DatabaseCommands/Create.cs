using Microsoft.EntityFrameworkCore;

namespace Corely.IAM.DataAccessMigrations.Cli.Commands.DatabaseCommands;

internal class Create() : DbCommandBase("create", "Create the database and apply all migrations")
{
    protected override async Task ExecuteAsync()
    {
        if (!TryCreateDbContext(out var dbContext))
            return;

        using (dbContext)
        {
            try
            {
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
}
