using Corely.IAM.PasswordRecoveries.Constants;
using Corely.IAM.PasswordRecoveries.Models;
using FluentValidation;

namespace Corely.IAM.PasswordRecoveries.Validators;

internal class RequestPasswordRecoveryRequestValidator
    : AbstractValidator<RequestPasswordRecoveryRequest>
{
    public RequestPasswordRecoveryRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty()
            .EmailAddress()
            .MaximumLength(PasswordRecoveryConstants.EMAIL_MAX_LENGTH);
    }
}
