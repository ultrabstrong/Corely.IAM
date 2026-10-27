using Corely.IAM.Roles.Models;

namespace Corely.IAM.WebApp.Api;

internal record RoleSummary(Guid Id, string Name, string? Description, bool IsSystemDefined)
{
    public static RoleSummary From(Role role) =>
        new(role.Id, role.Name, role.Description, role.IsSystemDefined);
}
