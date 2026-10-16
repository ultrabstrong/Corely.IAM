namespace Corely.IAM.Validators;

internal class ValidationResult
{
    public string Message { get; set; } = null!;
    public List<ValidationError>? Errors { get; init; }
    public bool IsValid => Errors == null || Errors.Count == 0;
}
