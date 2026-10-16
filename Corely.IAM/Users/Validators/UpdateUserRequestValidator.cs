using Corely.IAM.Users.Models;
using FluentValidation;

namespace Corely.IAM.Users.Validators;

internal class UpdateUserRequestValidator : AbstractValidator<UpdateUserRequest>
{
    public UpdateUserRequestValidator()
    {
        RuleFor(m => m.Username)
            .NotEmpty()
            .MinimumLength(User.USERNAME_MIN_LENGTH)
            .MaximumLength(User.USERNAME_MAX_LENGTH);
        RuleFor(m => m.Email).NotEmpty().EmailAddress().MaximumLength(User.EMAIL_MAX_LENGTH);
    }
}
