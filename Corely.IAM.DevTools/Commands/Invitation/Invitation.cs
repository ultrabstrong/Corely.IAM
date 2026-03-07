namespace Corely.IAM.DevTools.Commands.Invitation;

internal partial class Invitation : CommandBase
{
    public Invitation()
        : base("invitation", "Invitation operations") { }

    protected override void Execute()
    {
        Console.WriteLine("Sub command missing. Use --help to see the available sub commands");
    }
}
