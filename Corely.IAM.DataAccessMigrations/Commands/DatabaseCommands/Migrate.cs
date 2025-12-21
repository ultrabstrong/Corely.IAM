using Corely.IAM.DataAccess;
using Corely.IAM.DataAccessMigrations.Attributes;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;

namespace Corely.IAM.DataAccessMigrations.Commands.DatabaseCommands;

internal class Migrate(IServiceProvider serviceProvider)
    : CommandBase("migrate", "Apply pending migrations to the database")
{
    [Argument("Target migration name. Use '0' to revert all migrations.", isRequired: false)]
    private string TargetMigration { get; init; } = null!;

    protected override async Task ExecuteAsync()
    {
        if (!await ValidateConnectionAsync(serviceProvider))
            return;

        try
        {
            using var dbContext = serviceProvider.GetRequiredService<IamDbContext>();

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
