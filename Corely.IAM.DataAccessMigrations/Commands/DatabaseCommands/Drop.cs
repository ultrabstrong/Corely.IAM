using Corely.IAM.DataAccess;
using Corely.IAM.DataAccessMigrations.Attributes;
using Microsoft.Extensions.DependencyInjection;

namespace Corely.IAM.DataAccessMigrations.Commands.DatabaseCommands;

internal class Drop(IServiceProvider serviceProvider)
    : CommandBase("drop", "Drop the database (DANGEROUS)")
{
    [Option("-f", "--force", Description = "Skip confirmation prompt")]
    private bool Force { get; init; }

    protected override async Task ExecuteAsync()
    {
        if (!Force)
        {
            Warn("WARNING: This will permanently delete the database and all data!");
            Console.Write("Type 'DROP' to confirm: ");
            var confirmation = Console.ReadLine();

            if (confirmation != "DROP")
            {
                Info("Operation cancelled.");
                return;
            }
        }

        if (!await ValidateConnectionAsync(serviceProvider))
            return;

        try
        {
            using var dbContext = serviceProvider.GetRequiredService<IamDbContext>();

            Info("Dropping database...");
            var deleted = await dbContext.Database.EnsureDeletedAsync();

            if (deleted)
            {
                Success("Database dropped successfully.");
            }
            else
            {
                Warn("Database did not exist.");
            }
        }
        catch (Exception ex)
        {
            Error($"Failed to drop database: {ex.Message}");
        }
    }
}
