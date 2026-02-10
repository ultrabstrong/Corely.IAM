using Corely.IAM.Models;

namespace Corely.IAM.Roles.Models;

public class Role
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public string? Description { get; set; } = null!;
    public bool IsSystemDefined { get; internal set; }
    public Guid AccountId { get; set; }

    // Hydration properties — populated only when hydrate=true
    public List<ChildRef>? Users { get; set; }
    public List<ChildRef>? Groups { get; set; }
    public List<ChildRef>? Permissions { get; set; }
}
