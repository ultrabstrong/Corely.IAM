namespace Corely.IAM.Validators;

public class ValidationError
{
    public string Message { get; init; } = null!;
    public string PropertyName { get; init; } = null!;
}
