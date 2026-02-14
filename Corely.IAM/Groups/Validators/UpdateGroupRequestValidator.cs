using Corely.IAM.Groups.Constants;
using Corely.IAM.Groups.Models;
using FluentValidation;

namespace Corely.IAM.Groups.Validators;

internal class UpdateGroupRequestValidator : AbstractValidator<UpdateGroupRequest>
{
    public UpdateGroupRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MinimumLength(GroupConstants.GROUP_NAME_MIN_LENGTH)
            .MaximumLength(GroupConstants.GROUP_NAME_MAX_LENGTH);
    }
}
