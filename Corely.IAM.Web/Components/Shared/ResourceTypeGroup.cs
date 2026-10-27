using Corely.IAM.Permissions.Models;

namespace Corely.IAM.Web.Components.Shared;

internal sealed record ResourceTypeGroup(string Name, IReadOnlyList<ResourceTypeInfo> Types)
{
    public const string UNGROUPED = "Other";
}
