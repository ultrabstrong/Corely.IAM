using Corely.IAM.Security.Constants;

namespace Corely.IAM.Permissions.Models;

public record ResourceTypeInfo(
    string Name,
    string Description,
    IReadOnlyList<AuthAction> OwnerActions
)
{
    public string OwnerDefaultDescription() =>
        PermissionLabelProvider.GetName(
            Name,
            Guid.Empty,
            OwnerActions.Contains(AuthAction.Create),
            OwnerActions.Contains(AuthAction.Read),
            OwnerActions.Contains(AuthAction.Update),
            OwnerActions.Contains(AuthAction.Delete),
            OwnerActions.Contains(AuthAction.Execute)
        );
}
