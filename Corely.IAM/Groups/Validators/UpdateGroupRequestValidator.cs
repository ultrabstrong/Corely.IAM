using Corely.IAM.Groups.Models;
using FluentValidation;

namespace Corely.IAM.Groups.Validators;

internal class UpdateGroupRequestValidator : AbstractValidator<UpdateGroupRequest>
{
    public UpdateGroupRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MinimumLength(Group.NAME_MIN_LENGTH)
            .MaximumLength(Group.NAME_MAX_LENGTH);
    }
}
