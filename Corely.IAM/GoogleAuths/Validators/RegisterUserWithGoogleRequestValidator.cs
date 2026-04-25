using Corely.IAM.GoogleAuths.Models;
using FluentValidation;

namespace Corely.IAM.GoogleAuths.Validators;

internal class RegisterUserWithGoogleRequestValidator
    : AbstractValidator<RegisterUserWithGoogleRequest>
{
    public RegisterUserWithGoogleRequestValidator()
    {
        RuleFor(x => x.GoogleIdToken).NotEmpty();
    }
}
