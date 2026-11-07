using Corely.IAM.IdentityProviders.Models;
using FluentValidation;

namespace Corely.IAM.IdentityProviders.Validators;

internal class UpdateIdentityProviderRequestValidator
    : AbstractValidator<UpdateIdentityProviderRequest>
{
    public UpdateIdentityProviderRequestValidator()
    {
        RuleFor(x => x.Type).IsInEnum();
        RuleFor(x => x.CredentialKind).IsInEnum();
        RuleFor(x => x.ClientId).MaximumLength(IdentityProviderSettings.CLIENT_ID_MAX_LENGTH);
        RuleFor(x => x.ClientId).NotEmpty().When(x => x.Enabled);
        RuleFor(x => x.Tenant).MaximumLength(IdentityProviderSettings.TENANT_MAX_LENGTH);
    }
}
