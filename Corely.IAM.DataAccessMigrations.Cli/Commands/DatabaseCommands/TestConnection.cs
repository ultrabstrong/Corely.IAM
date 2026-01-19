using Corely.IAM.DataAccess;
using Microsoft.Extensions.DependencyInjection;

namespace Corely.IAM.DataAccessMigrations.Cli.Commands.DatabaseCommands;

internal class TestConnection(IServiceProvider serviceProvider)
    : CommandBase("test-connection", "Test the database connection")
{
    protected override async Task ExecuteAsync()
    {
        if (!ValidateSettings(out _))
            return;

        try
        {
            using var dbContext = serviceProvider.GetRequiredService<IamDbContext>();

            Info("Testing database connection...");
            var canConnect = await dbContext.Database.CanConnectAsync();

            if (canConnect)
            {
                Success("Successfully connected to the database.");
            }
            else
            {
                Error("Could not connect to the database.");
                Info(
                    "Verify the connection string in your settings file is correct. Run 'config show' to view current settings."
                );
            }
        }
        catch (Exception ex)
        {
            Error($"Connection test failed: {ex.Message}");
            Info("Check that the database server is running and the connection string is correct.");
        }
    }
}
