using Corely.IAM.Invitations.Constants;
using Corely.IAM.Invitations.Models;
using FluentValidation;

namespace Corely.IAM.Invitations.Validators;

internal class CreateInvitationRequestValidator : AbstractValidator<CreateInvitationRequest>
{
    public CreateInvitationRequestValidator()
    {
        RuleFor(x => x.AccountId).NotEmpty();

        RuleFor(x => x.Email)
            .NotEmpty()
            .EmailAddress()
            .MaximumLength(InvitationConstants.EMAIL_MAX_LENGTH);

        RuleFor(x => x.Description)
            .MaximumLength(InvitationConstants.DESCRIPTION_MAX_LENGTH)
            .When(x => x.Description != null);

        RuleFor(x => x.ExpiresInSeconds)
            .InclusiveBetween(
                InvitationConstants.MIN_EXPIRY_SECONDS,
                InvitationConstants.MAX_EXPIRY_SECONDS
            );
    }
}
