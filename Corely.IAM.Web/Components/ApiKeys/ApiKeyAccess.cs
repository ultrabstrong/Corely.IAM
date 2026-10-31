using Corely.IAM.ApiKeys.Models;
using Corely.IAM.Models;
using Corely.IAM.Platform.Models;

namespace Corely.IAM.Web.Components.ApiKeys;

public sealed record ApiKeyAccess(
    IReadOnlyList<string> AllowedTypes,
    IReadOnlyList<ApiKeyTypeAccess> Types
)
{
    public bool HasUsableAccess => Types.Any(t => t.Allowed);

    public static ApiKeyAccess Of(ApiKey key, ResourceTypeSet allowed) =>
        new(
            allowed.Types,
            [
                .. (key.EffectivePermissions ?? [])
                    .GroupBy(p => p.ResourceType, StringComparer.OrdinalIgnoreCase)
                    .Select(g => new ApiKeyTypeAccess(g.Key, allowed.Contains(g.Key), [.. g]))
                    .OrderByDescending(t => t.Allowed)
                    .ThenBy(t => t.ResourceType, StringComparer.OrdinalIgnoreCase),
            ]
        );
}
