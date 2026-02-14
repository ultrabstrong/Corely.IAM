using Corely.IAM.Users.Constants;
using Corely.IAM.Users.Models;
using FluentValidation;

namespace Corely.IAM.Users.Validators;

internal class UpdateUserRequestValidator : AbstractValidator<UpdateUserRequest>
{
    public UpdateUserRequestValidator()
    {
        RuleFor(m => m.Username)
            .NotEmpty()
            .MinimumLength(UserConstants.USERNAME_MIN_LENGTH)
            .MaximumLength(UserConstants.USERNAME_MAX_LENGTH);
        RuleFor(m => m.Email)
            .NotEmpty()
            .EmailAddress()
            .MaximumLength(UserConstants.EMAIL_MAX_LENGTH);
    }
}
