using FluentValidation.Results;
using CorelyValidationError = Corely.IAM.Validators.ValidationError;
using CorelyValidationResult = Corely.IAM.Validators.ValidationResult;

namespace Corely.IAM.Validators.Mappers;

internal static class ValidationMapper
{
    public static CorelyValidationError ToValidationError(this ValidationFailure failure)
    {
        return new CorelyValidationError
        {
            Message = failure.ErrorMessage,
            PropertyName = failure.PropertyName,
        };
    }

    public static CorelyValidationResult ToValidationResult(
        this FluentValidation.Results.ValidationResult fluentResult
    )
    {
        return new CorelyValidationResult
        {
            Errors = fluentResult.Errors?.Select(e => e.ToValidationError()).ToList(),
        };
    }
}
