using Corely.IAM.Models;

namespace Corely.IAM.Groups.Models;

public class Group
{
    public const string RESOURCE_TYPE = "group";
    public const int NAME_MIN_LENGTH = 1;
    public const int NAME_MAX_LENGTH = 100;

    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public string? Description { get; set; } = null!;
    public Guid AccountId { get; set; }
    public List<ChildRef>? Users { get; set; }
    public List<ChildRef>? Roles { get; set; }
}
