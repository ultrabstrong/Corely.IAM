using Corely.IAM.Invitations.Models;
using Corely.IAM.Users.Models;
using FluentValidation;

namespace Corely.IAM.Invitations.Validators;

internal class CreateInvitationRequestValidator : AbstractValidator<CreateInvitationRequest>
{
    public CreateInvitationRequestValidator()
    {
        RuleFor(x => x.AccountId).NotEmpty();

        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(User.EMAIL_MAX_LENGTH);

        RuleFor(x => x.Description)
            .MaximumLength(Invitation.DESCRIPTION_MAX_LENGTH)
            .When(x => x.Description != null);

        RuleFor(x => x.ExpiresInSeconds)
            .InclusiveBetween(
                CreateInvitationRequest.MIN_EXPIRY_SECONDS,
                CreateInvitationRequest.MAX_EXPIRY_SECONDS
            );
    }
}
