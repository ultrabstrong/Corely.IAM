namespace Corely.IAM.DevTools.Commands.Password;

internal partial class Password : CommandBase
{
    public Password()
        : base("password", "Password operations for the current user") { }

    protected override void Execute()
    {
        Console.WriteLine("Sub command missing. Use --help to see the available sub commands");
    }
}
