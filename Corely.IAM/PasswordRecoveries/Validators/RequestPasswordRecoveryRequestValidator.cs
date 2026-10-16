using Corely.IAM.PasswordRecoveries.Models;
using Corely.IAM.Users.Models;
using FluentValidation;

namespace Corely.IAM.PasswordRecoveries.Validators;

internal class RequestPasswordRecoveryRequestValidator
    : AbstractValidator<RequestPasswordRecoveryRequest>
{
    public RequestPasswordRecoveryRequestValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(User.EMAIL_MAX_LENGTH);
    }
}
