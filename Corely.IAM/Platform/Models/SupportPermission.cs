using Corely.IAM.Permissions.Entities;
using Corely.IAM.Security.Enums;

namespace Corely.IAM.Platform.Models;

internal sealed record SupportPermission(string ResourceType, IReadOnlyList<AuthAction> Actions)
{
    public bool Matches(PermissionEntity permission) =>
        permission.ResourceType == ResourceType
        && permission.ResourceId == Guid.Empty
        && permission.Create == Actions.Contains(AuthAction.Create)
        && permission.Read == Actions.Contains(AuthAction.Read)
        && permission.Update == Actions.Contains(AuthAction.Update)
        && permission.Delete == Actions.Contains(AuthAction.Delete)
        && permission.Execute == Actions.Contains(AuthAction.Execute);
}
