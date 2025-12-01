namespace Corely.IAM.Roles.Models;

public class Role
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public string? Description { get; set; } = null!;
    public bool IsSystemDefined { get; internal set; }
    public int AccountId { get; set; }
}
