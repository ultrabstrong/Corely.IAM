using Corely.IAM.Permissions.Models;
using Corely.IAM.Permissions.Providers;
using FluentValidation;

namespace Corely.IAM.Permissions.Validators;

internal class PermissionValidator : AbstractValidator<Permission>
{
    public PermissionValidator(IResourceTypeRegistry resourceTypeRegistry)
    {
        RuleFor(x => x.ResourceType).NotEmpty();

        RuleFor(x => x.ResourceType)
            .Must(resourceType => resourceTypeRegistry.Exists(resourceType))
            .When(x => !string.IsNullOrWhiteSpace(x.ResourceType))
            .WithMessage(x => $"Resource type '{x.ResourceType}' is not registered");

        RuleFor(x => x)
            .Must(x => x.Create || x.Read || x.Update || x.Delete || x.Execute)
            .WithMessage(
                "At least one permission (Create, Read, Update, Delete, Execute) must be set"
            );
    }
}
