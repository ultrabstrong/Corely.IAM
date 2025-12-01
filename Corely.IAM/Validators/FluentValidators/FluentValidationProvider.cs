using AutoMapper;

namespace Corely.IAM.Validators.FluentValidators;

internal sealed class FluentValidationProvider : IValidationProvider
{
    private readonly IFluentValidatorFactory _fluentValidatorFactory;
    private readonly IMapper _mapper;

    public FluentValidationProvider(IFluentValidatorFactory fluentValidatorFactory, IMapper mapper)
    {
        _fluentValidatorFactory = fluentValidatorFactory;
        _mapper = mapper;
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
            corelyResult = _mapper.Map<ValidationResult>(fluentResult);
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
