namespace Corely.IAM.DevTools.Commands.Modification;

internal partial class Modification : CommandBase
{
    public Modification()
        : base("modify", "Modification service commands for updating entities") { }

    protected override void Execute()
    {
        Console.WriteLine("Sub command missing. Use --help to see the available sub commands");
    }
}
