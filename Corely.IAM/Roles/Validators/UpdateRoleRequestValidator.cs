using Corely.IAM.Roles.Models;
using FluentValidation;

namespace Corely.IAM.Roles.Validators;

internal class UpdateRoleRequestValidator : AbstractValidator<UpdateRoleRequest>
{
    public UpdateRoleRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MinimumLength(Role.NAME_MIN_LENGTH)
            .MaximumLength(Role.NAME_MAX_LENGTH);
    }
}
