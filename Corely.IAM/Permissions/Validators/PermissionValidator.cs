using Corely.IAM.Permissions.Models;
using FluentValidation;

namespace Corely.IAM.Permissions.Validators;

internal class PermissionValidator : AbstractValidator<Permission>
{
    public PermissionValidator()
    {
        RuleFor(x => x.ResourceType).NotEmpty();

        RuleFor(x => x.ResourceId).GreaterThanOrEqualTo(0);

        RuleFor(x => x)
            .Must(x => x.Create || x.Read || x.Update || x.Delete || x.Execute)
            .WithMessage(
                "At least one permission (Create, Read, Update, Delete, Execute) must be set"
            );
    }
}
