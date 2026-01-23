namespace Corely.IAM.DevTools.Commands.Provider;

internal partial class Provider : CommandBase
{
    public Provider()
        : base("provider", "Database provider management commands") { }

    protected override void Execute()
    {
        Console.WriteLine("Sub command missing. Use --help to see the available sub commands");
    }
}
