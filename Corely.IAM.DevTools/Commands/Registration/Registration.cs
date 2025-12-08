namespace Corely.IAM.DevTools.Commands.Registration;

internal partial class Registration : CommandBase
{
    public Registration()
        : base("register", "Register operations") { }

    protected override void Execute()
    {
        Console.WriteLine("Sub command missing. Use --help to see the available sub commands");
    }
}
