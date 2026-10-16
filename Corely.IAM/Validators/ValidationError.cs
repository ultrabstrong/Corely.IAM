namespace Corely.IAM.Validators;

internal class ValidationError
{
    public string Message { get; init; } = null!;
    public string PropertyName { get; init; } = null!;
}
