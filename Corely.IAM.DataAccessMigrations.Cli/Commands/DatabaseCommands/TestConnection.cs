namespace Corely.IAM.DataAccessMigrations.Cli.Commands.DatabaseCommands;

internal class TestConnection() : DbCommandBase("test-connection", "Test the database connection")
{
    protected override async Task ExecuteAsync()
    {
        if (!TryCreateDbContext(out var dbContext))
            return;

        using (dbContext)
        {
            Info("Testing database connection...");
            if (await TryConnectAsync(dbContext))
            {
                Success("Successfully connected to the database.");
            }
        }
    }
}
