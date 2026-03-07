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

    public ValidationResult ValidateAndLog<T>(T model)
    {
        var result = Validate(model);
        if (!result.IsValid)
        {
            var state = new Dictionary<string, object?> { { "@ValidationResult", result } };
            using var scope = _logger.BeginScope(state);
            _logger.LogWarning(
                "Validation failed for {ModelType}: {Message}",
                typeof(T).Name,
                result.Message
            );
        }
        return result;
    }

    public void ThrowIfInvalid<T>(T model)
    {
        var result = ValidateAndLog(model);
        if (!result.IsValid)
        {
            throw new ValidationException(result.Message) { ValidationResult = result };
        }
    }
}
