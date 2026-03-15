namespace Corely.IAM.DevTools.Commands.Totp;

internal partial class Totp : CommandBase
{
    public Totp()
        : base("totp", "TOTP multi-factor authentication operations") { }

    protected override void Execute()
    {
        Console.WriteLine("Sub command missing. Use --help to see the available sub commands");
    }
}
