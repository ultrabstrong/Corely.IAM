using Corely.IAM.Roles.Constants;
using Corely.IAM.Roles.Models;
using FluentValidation;

namespace Corely.IAM.Roles.Validators;

internal class UpdateRoleRequestValidator : AbstractValidator<UpdateRoleRequest>
{
    public UpdateRoleRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MinimumLength(RoleConstants.ROLE_NAME_MIN_LENGTH)
            .MaximumLength(RoleConstants.ROLE_NAME_MAX_LENGTH);
    }
}
