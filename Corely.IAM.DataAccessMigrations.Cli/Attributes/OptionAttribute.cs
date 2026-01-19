namespace Corely.IAM.DataAccessMigrations.Cli.Attributes;

[AttributeUsage(AttributeTargets.Property)]
internal class OptionAttribute : AttributeBase
{
    public string[] Aliases { get; init; }

    public OptionAttribute(params string[] aliases)
    {
        Aliases = aliases;
    }
}
