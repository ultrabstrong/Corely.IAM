using Corely.IAM.Permissions.Models;
using Corely.IAM.Web.Components.Shared;

namespace Corely.IAM.Web.Extensions;

internal static class ResourceTypeInfoExtensions
{
    extension(IEnumerable<ResourceTypeInfo> types)
    {
        public IReadOnlyList<ResourceTypeGroup> Grouped(string? search = null) =>
            [
                .. types
                    .Where(t =>
                        string.IsNullOrWhiteSpace(search)
                        || t.Title.Contains(search.Trim(), StringComparison.OrdinalIgnoreCase)
                        || t.Description.Contains(search.Trim(), StringComparison.OrdinalIgnoreCase)
                        || t.Name.Contains(search.Trim(), StringComparison.OrdinalIgnoreCase)
                    )
                    .GroupBy(t => t.Group ?? ResourceTypeGroup.UNGROUPED)
                    .OrderBy(g => GroupRank(g.Key))
                    .ThenBy(g => g.Key, StringComparer.OrdinalIgnoreCase)
                    .Select(g => new ResourceTypeGroup(
                        g.Key,
                        [.. g.OrderBy(t => t.Title, StringComparer.OrdinalIgnoreCase)]
                    )),
            ];
    }

    extension(ResourceTypeInfo type)
    {
        public string GroupedTitle =>
            type.Group is null or ResourceTypeInfo.IDENTITY_AND_ACCESS_GROUP
                ? type.Title
                : $"{type.Group}: {type.Title}";
    }

    private static int GroupRank(string group) =>
        group switch
        {
            ResourceTypeInfo.IDENTITY_AND_ACCESS_GROUP => 0,
            ResourceTypeInfo.PLATFORM_SETTINGS_GROUP => 2,
            ResourceTypeGroup.UNGROUPED => 3,
            _ => 1,
        };
}
