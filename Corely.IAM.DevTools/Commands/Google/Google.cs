namespace Corely.IAM.DevTools.Commands.Google;

internal partial class Google : CommandBase
{
    public Google()
        : base("google", "Google authentication operations") { }

    protected override void Execute()
    {
        Console.WriteLine("Sub command missing. Use --help to see the available sub commands");
    }
}
