using Corely.IAM.Validators.Mappers;
using Microsoft.Extensions.Logging;

namespace Corely.IAM.Validators.FluentValidators;

internal sealed class FluentValidationProvider(
    IFluentValidatorFactory fluentValidatorFactory,
    ILogger<FluentValidationProvider> logger
) : IValidationProvider
{
    private readonly IFluentValidatorFactory _fluentValidatorFactory = fluentValidatorFactory;
    private readonly ILogger<FluentValidationProvider> _logger = logger;

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
        try
        {
            Validate(model).ThrowIfInvalid();
        }
        catch (Exception ex)
        {
            var state = new Dictionary<string, object?>();

            if (
                ex is ValidationException validationException
                && validationException.ValidationResult != null
            )
            {
                state.Add("@ValidationResult", validationException.ValidationResult);
            }

            using var scope = _logger.BeginScope(state);
            _logger.LogWarning(ex, "Validation failed for {ModelType}", model?.GetType()?.Name);
            throw;
        }
    }
}
