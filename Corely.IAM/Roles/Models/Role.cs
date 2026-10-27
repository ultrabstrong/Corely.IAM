using Corely.IAM.Models;

namespace Corely.IAM.Roles.Models;

public class Role
{
    public const string RESOURCE_TYPE = "role";
    public const string OWNER_ROLE_NAME = "Owner Role";
    public const int NAME_MIN_LENGTH = 1;
    public const int NAME_MAX_LENGTH = 100;

    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public string? Description { get; set; } = null!;
    public bool IsSystemDefined { get; internal set; }
    public Guid AccountId { get; set; }

    public List<ChildRef>? Users { get; set; }
    public List<ChildRef>? Groups { get; set; }
    public List<ChildRef>? ApiKeys { get; set; }
    public List<ChildRef>? Permissions { get; set; }
}
