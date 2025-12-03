using Corely.IAM.Validators.Mappers;

namespace Corely.IAM.Validators.FluentValidators;

internal sealed class FluentValidationProvider : IValidationProvider
{
    private readonly IFluentValidatorFactory _fluentValidatorFactory;

    public FluentValidationProvider(IFluentValidatorFactory fluentValidatorFactory)
    {
        _fluentValidatorFactory = fluentValidatorFactory;
    }

    public ValidationResult Validate<T>(T model)
    {
        ValidationResult corelyResult;
        if (model == null)
        {
            corelyResult = new()
            {
                Errors = [new() { Message = "Model is null", PropertyName = typeof(T).Name }],
            };
        }
        else
        {
            var validator = _fluentValidatorFactory.GetValidator<T>();
            var fluentResult = validator.Validate(model);
            corelyResult = fluentResult.ToValidationResult();
        }

        corelyResult.Message =
            $"Validation for {typeof(T).Name} {(corelyResult.IsValid ? "succeeded" : "failed")}";

        return corelyResult;
    }

    public void ThrowIfInvalid<T>(T model)
    {
        Validate(model).ThrowIfInvalid();
    }
}
