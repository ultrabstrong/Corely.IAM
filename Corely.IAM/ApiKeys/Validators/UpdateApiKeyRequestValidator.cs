using Corely.IAM.ApiKeys.Models;
using FluentValidation;

namespace Corely.IAM.ApiKeys.Validators;

internal class UpdateApiKeyRequestValidator : AbstractValidator<UpdateApiKeyRequest>
{
    public UpdateApiKeyRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MinimumLength(ApiKey.NAME_MIN_LENGTH)
            .MaximumLength(ApiKey.NAME_MAX_LENGTH);
        RuleFor(x => x.Description).MaximumLength(ApiKey.DESCRIPTION_MAX_LENGTH);
    }
}
