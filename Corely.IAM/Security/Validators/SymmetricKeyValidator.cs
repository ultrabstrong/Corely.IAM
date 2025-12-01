using Corely.IAM.Security.Constants;
using Corely.IAM.Security.Models;
using FluentValidation;

namespace Corely.IAM.Security.Validators;

internal class SymmetricKeyValidator : AbstractValidator<SymmetricKey>
{
    public SymmetricKeyValidator()
    {
        RuleFor(m => m.Key).NotNull();

        RuleFor(m => m.Key.Secret)
            .NotEmpty()
            .MaximumLength(SymmetricKeyConstants.KEY_MAX_LENGTH)
            .When(m => m.Key != null);

        RuleFor(m => m.Version).GreaterThanOrEqualTo(SymmetricKeyConstants.VERSION_MIN_VALUE);

        RuleFor(m => m.ProviderTypeCode).NotEmpty();
    }
}
