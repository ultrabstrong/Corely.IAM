namespace Corely.IAM.Validators;

public class ValidationResult
{
    public string Message { get; set; } = null!;
    public List<ValidationError>? Errors { get; init; }
    public bool IsValid => Errors == null || Errors.Count == 0;

    public void ThrowIfInvalid()
    {
        if (!IsValid)
        {
            throw new ValidationException(Message) { ValidationResult = this };
        }
    }
}
