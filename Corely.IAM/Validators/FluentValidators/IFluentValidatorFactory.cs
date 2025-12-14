using FluentValidation;

namespace Corely.IAM.Validators.FluentValidators;

internal interface IFluentValidatorFactory
{
    IValidator<T> GetValidator<T>();
}
