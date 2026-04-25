using Corely.IAM.GoogleAuths.Models;
using FluentValidation;

namespace Corely.IAM.GoogleAuths.Validators;

internal class LinkGoogleAuthRequestValidator : AbstractValidator<LinkGoogleAuthRequest>
{
    public LinkGoogleAuthRequestValidator()
    {
        RuleFor(x => x.GoogleIdToken).NotEmpty();
    }
}
