namespace Corely.IAM.DataAccessMigrations.Cli.Commands.ProviderCommands;

internal class List() : CommandBase("list", "List available database providers")
{
    protected override Task ExecuteAsync()
    {
        Info("Available database providers:");
        foreach (var provider in DatabaseProviderExtensions.GetNames())
        {
            Info($"  - {provider}");
        }
        return Task.CompletedTask;
    }
}
