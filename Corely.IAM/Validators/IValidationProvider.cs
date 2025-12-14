namespace Corely.IAM.Validators;

internal interface IValidationProvider
{
    public ValidationResult Validate<T>(T model);

    public void ThrowIfInvalid<T>(T model);
}
