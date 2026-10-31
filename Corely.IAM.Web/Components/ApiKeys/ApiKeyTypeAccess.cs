using Corely.IAM.Models;

namespace Corely.IAM.Web.Components.ApiKeys;

public sealed record ApiKeyTypeAccess(
    string ResourceType,
    bool Allowed,
    List<EffectivePermission> Permissions
);
