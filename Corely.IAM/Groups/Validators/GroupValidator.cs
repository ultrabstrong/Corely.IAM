using Corely.IAM.Groups.Models;
using FluentValidation;

namespace Corely.IAM.Groups.Validators;

internal class GroupValidator : AbstractValidator<Group>
{
    public GroupValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MinimumLength(Group.NAME_MIN_LENGTH)
            .MaximumLength(Group.NAME_MAX_LENGTH);
    }
}
