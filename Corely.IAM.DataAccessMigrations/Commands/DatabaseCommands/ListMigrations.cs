using Corely.IAM.DataAccess;
using Corely.IAM.DataAccessMigrations.Attributes;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;

namespace Corely.IAM.DataAccessMigrations.Commands.DatabaseCommands;

internal class ListMigrations(IServiceProvider serviceProvider)
    : CommandBase("list", "List all available migrations")
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
            ).ToHashSet();
            var pendingMigrations = (
                ShowAll
                    ? allPendingMigrations
                    : allPendingMigrations.Where(m => localMigrations.Contains(m))
            ).ToList();
            var allMigrations = appliedMigrations.Union(pendingMigrations).OrderBy(m => m);

            Info(ShowAll ? "=== All Migrations (All Projects) ===" : "=== All Migrations ===");
            Info("");

            foreach (var migration in allMigrations)
            {
                var isApplied = appliedMigrations.Contains(migration);
                var isLocal = localMigrations.Contains(migration);
                var status = isApplied ? "Applied" : "Pending";

                // Mark non-local migrations when showing all
                var suffix = ShowAll && !isLocal ? " (other project)" : "";

                var color = isApplied ? ConsoleColor.Green : ConsoleColor.Yellow;
                WriteColored($"  [{status}] {migration}{suffix}", color);
            }

            Info("");
            Info(
                $"Total: {allMigrations.Count()} migrations ({appliedMigrations.Count} applied, {pendingMigrations.Count} pending)"
            );
        }
        catch (Exception ex)
        {
            Error($"Failed to list migrations: {ex.Message}");
        }
    }
}
