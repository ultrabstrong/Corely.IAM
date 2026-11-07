namespace Corely.IAM.DevTools.Commands.Identity;

internal partial class Identity : CommandBase
{
    public Identity()
        : base("identity", "Linked identity provider accounts of the signed in user") { }

    protected override void Execute()
    {
        Console.WriteLine("Sub command missing. Use --help to see the available sub commands");
    }
}
