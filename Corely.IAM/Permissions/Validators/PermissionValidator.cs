using Corely.IAM.Permissions.Constants;
using Corely.IAM.Permissions.Models;
using FluentValidation;

namespace Corely.IAM.Permissions.Validators;

internal class PermissionValidator : AbstractValidator<Permission>
{
    public PermissionValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MinimumLength(PermissionConstants.PERMISSION_NAME_MIN_LENGTH)
            .MaximumLength(PermissionConstants.PERMISSION_NAME_MAX_LENGTH);
    }
}
