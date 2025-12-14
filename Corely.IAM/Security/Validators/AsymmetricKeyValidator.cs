using Corely.IAM.Security.Constants;
using Corely.IAM.Security.Models;
using FluentValidation;

namespace Corely.IAM.Security.Validators;

internal class AsymmetricKeyValidator : AbstractValidator<AsymmetricKey>
{
    public AsymmetricKeyValidator()
    {
        RuleFor(m => m.PublicKey).NotEmpty();

        RuleFor(m => m.PrivateKey).NotNull();

        RuleFor(m => m.PrivateKey.Secret).NotEmpty().When(m => m.PrivateKey != null);

        RuleFor(m => m.Version).GreaterThanOrEqualTo(AsymmetricKeyConstants.VERSION_MIN_VALUE);

        RuleFor(m => m.ProviderTypeCode).NotEmpty();
    }
}
