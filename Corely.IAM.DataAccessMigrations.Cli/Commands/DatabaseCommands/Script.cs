using Corely.IAM.DataAccess;
using Corely.IAM.DataAccessMigrations.Cli.Attributes;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;

namespace Corely.IAM.DataAccessMigrations.Cli.Commands.DatabaseCommands;

internal class Script(IServiceProvider serviceProvider)
    : CommandBase("script", "Generate a SQL script from migrations")
{
    [Argument("Source migration (use '0' for initial state)", isRequired: false)]
    private string FromMigration { get; init; } = null!;

    [Argument("Target migration (defaults to latest)", isRequired: false)]
    private string ToMigration { get; init; } = null!;

    [Option(
        "-o",
        "--output",
        Description = "Output file path (prints to console if not specified)"
    )]
    private string OutputFile { get; init; } = null!;

    [Option(
        "-i",
        "--idempotent",
        Description = "Generate an idempotent script that can be run multiple times"
    )]
    private bool Idempotent { get; init; }

    protected override async Task ExecuteAsync()
    {
        if (!ValidateSettings(out _))
            return;

        try
        {
            using var dbContext = serviceProvider.GetRequiredService<IamDbContext>();

            var migrator = dbContext.Database.GetInfrastructure().GetRequiredService<IMigrator>();

            var fromMigration = string.IsNullOrEmpty(FromMigration) ? null : FromMigration;
            var toMigration = string.IsNullOrEmpty(ToMigration) ? null : ToMigration;

            Info($"Generating SQL script...");
            if (fromMigration != null)
                Info($"  From: {fromMigration}");
            if (toMigration != null)
                Info($"  To: {toMigration}");
            if (Idempotent)
                Info("  Mode: Idempotent");

            var script = migrator.GenerateScript(
                fromMigration,
                toMigration,
                Idempotent
                    ? MigrationsSqlGenerationOptions.Idempotent
                    : MigrationsSqlGenerationOptions.Default
            );

            if (!string.IsNullOrEmpty(OutputFile))
            {
                await File.WriteAllTextAsync(OutputFile, script);
                Success($"SQL script saved to: {OutputFile}");
            }
            else
            {
                Info("");
                Info("=== Generated SQL Script ===");
                Info(script);
            }
        }
        catch (Exception ex)
        {
            Error($"Failed to generate script: {ex.Message}");
        }
    }
}
