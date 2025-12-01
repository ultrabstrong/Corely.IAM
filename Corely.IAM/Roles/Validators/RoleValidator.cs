using Corely.IAM.Roles.Constants;
using Corely.IAM.Roles.Models;
using FluentValidation;

namespace Corely.IAM.Roles.Validators;

internal class RoleValidator : AbstractValidator<Role>
{
    public RoleValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MinimumLength(RoleConstants.ROLE_NAME_MIN_LENGTH)
            .MaximumLength(RoleConstants.ROLE_NAME_MAX_LENGTH);
    }
}
