using Corely.IAM.DataAccess;
using Corely.IAM.DataAccessMigrations.Cli.Attributes;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;

namespace Corely.IAM.DataAccessMigrations.Cli.Commands.DatabaseCommands;

internal class Status(IServiceProvider serviceProvider)
    : CommandBase("status", "Show the migration status of the database")
{
    [Option(
        "-a",
        "--show-all",
        Description = "Show all migrations in the database, not just those from this project"
    )]
    private bool ShowAll { get; init; }

    protected override async Task ExecuteAsync()
    {
        if (!await ValidateConnectionAsync(serviceProvider))
            return;

        try
        {
            using var dbContext = serviceProvider.GetRequiredService<IamDbContext>();

            // Get migrations defined in this project's assembly
            var migrationsAssembly = dbContext
                .Database.GetInfrastructure()
                .GetRequiredService<IMigrationsAssembly>();
            var localMigrations = migrationsAssembly.Migrations.Keys.ToHashSet();

            var allAppliedMigrations = await dbContext.Database.GetAppliedMigrationsAsync();
            var allPendingMigrations = await dbContext.Database.GetPendingMigrationsAsync();

            // Filter to local migrations unless ShowAll is specified
            var appliedMigrations = (
                ShowAll
                    ? allAppliedMigrations
                    : allAppliedMigrations.Where(m => localMigrations.Contains(m))
            ).ToList();
            var pendingMigrations = (
                ShowAll
                    ? allPendingMigrations
                    : allPendingMigrations.Where(m => localMigrations.Contains(m))
            ).ToList();

            Info(
                ShowAll
                    ? "=== Database Migration Status (All Projects) ==="
                    : "=== Database Migration Status ==="
            );
            Info("");

            if (appliedMigrations.Any())
            {
                Success("Applied Migrations:");
                foreach (var migration in appliedMigrations)
                {
                    var suffix =
                        ShowAll && !localMigrations.Contains(migration) ? " (other project)" : "";
                    Info($"  {migration}{suffix}");
                }
            }
            else
            {
                Warn("No migrations have been applied yet.");
            }

            Info("");

            if (pendingMigrations.Any())
            {
                Warn("Pending Migrations:");
                foreach (var migration in pendingMigrations)
                {
                    var suffix =
                        ShowAll && !localMigrations.Contains(migration) ? " (other project)" : "";
                    Info($"  {migration}{suffix}");
                }
            }
            else
            {
                Success("All migrations are applied. Database is up to date.");
            }
        }
        catch (Exception ex)
        {
            Error($"Failed to get migration status: {ex.Message}");
        }
    }
}
