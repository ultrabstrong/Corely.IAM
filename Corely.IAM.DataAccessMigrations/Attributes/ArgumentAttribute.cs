namespace Corely.IAM.DataAccessMigrations.Attributes;

[AttributeUsage(AttributeTargets.Property)]
internal class ArgumentAttribute : AttributeBase
{
    public bool IsRequired { get; init; } = true;

    public ArgumentAttribute(string description)
    {
        Description = description;
    }

    public ArgumentAttribute(string description, bool isRequired)
        : this(description)
    {
        IsRequired = isRequired;
    }
}
