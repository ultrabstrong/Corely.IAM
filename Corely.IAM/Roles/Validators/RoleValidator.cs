using Corely.IAM.Roles.Models;
using FluentValidation;

namespace Corely.IAM.Roles.Validators;

internal class RoleValidator : AbstractValidator<Role>
{
    public RoleValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MinimumLength(Role.NAME_MIN_LENGTH)
            .MaximumLength(Role.NAME_MAX_LENGTH);
    }
}
