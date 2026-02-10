namespace Corely.IAM.Models;

public class EffectiveRole
{
    public Guid RoleId { get; init; }
    public string RoleName { get; init; } = null!;
    public bool IsDirect { get; init; }
    public List<EffectiveGroup> Groups { get; init; } = [];
}
