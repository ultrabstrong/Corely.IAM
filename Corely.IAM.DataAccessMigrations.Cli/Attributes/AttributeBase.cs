using System.CommandLine;

namespace Corely.IAM.DataAccessMigrations.Cli.Attributes;

internal abstract class AttributeBase : Attribute
{
    public string Description { get; init; } = null!;

    public ArgumentArity? ArgumentArity { get; init; }
}
