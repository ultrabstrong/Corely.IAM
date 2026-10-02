using Corely.IAM.Security.Constants;

namespace Corely.IAM.Permissions.Models;

internal record CreatePermissionRequest(
    Guid OwnerAccountId,
    string ResourceType,
    Guid ResourceId,
    bool Create = false,
    bool Read = false,
    bool Update = false,
    bool Delete = false,
    bool Execute = false,
    string? Description = null
)
{
    public AuthAction[] AllowedActions() =>
        [
            .. new (bool Allowed, AuthAction Action)[]
            {
                (Create, AuthAction.Create),
                (Read, AuthAction.Read),
                (Update, AuthAction.Update),
                (Delete, AuthAction.Delete),
                (Execute, AuthAction.Execute),
            }
                .Where(a => a.Allowed)
                .Select(a => a.Action),
        ];
}
