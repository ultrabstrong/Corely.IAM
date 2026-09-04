using Corely.IAM.DataAccessMigrations.Cli.Attributes;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;

namespace Corely.IAM.DataAccessMigrations.Cli.Commands.DatabaseCommands;

internal class Migrate() : DbCommandBase("migrate", "Apply pending migrations to the database")
{
    [Argument("Target migration name. Use '0' to revert all migrations.", isRequired: false)]
    private string TargetMigration { get; init; } = null!;

    protected override async Task ExecuteAsync()
    {
        if (!TryCreateDbContext(out var dbContext))
            return;

        using (dbContext)
        {
            if (!await TryConnectAsync(dbContext))
                return;

            try
            {
                if (string.IsNullOrEmpty(TargetMigration))
                {
                    Info("Applying all pending migrations...");
                    await dbContext.Database.MigrateAsync();
                    Success("All migrations applied successfully.");
                }
                else
                {
                    Info($"Migrating to: {TargetMigration}...");
                    var migrator = dbContext
                        .Database.GetInfrastructure()
                        .GetRequiredService<IMigrator>();
                    await migrator.MigrateAsync(TargetMigration);
                    Success($"Successfully migrated to: {TargetMigration}");
                }
            }
            catch (Exception ex)
            {
                Error($"Migration failed: {ex.Message}");
            }
        }
    }
}
