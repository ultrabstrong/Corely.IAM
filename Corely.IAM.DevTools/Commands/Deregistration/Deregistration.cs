namespace Corely.IAM.DevTools.Commands.Deregistration;

internal partial class Deregistration : CommandBase
{
    public Deregistration()
        : base("deregister", "Deregister operations") { }

    protected override void Execute()
    {
        Console.WriteLine("Sub command missing. Use --help to see the available sub commands");
    }
}
