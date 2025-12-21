namespace Corely.IAM.DevTools.Commands.Config;

internal partial class Config : CommandBase
{
    public Config()
        : base("config", "Configuration management commands") { }

    protected override void Execute()
    {
        Console.WriteLine("Sub command missing. Use --help to see the available sub commands");
    }
}
