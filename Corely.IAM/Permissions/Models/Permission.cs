namespace Corely.IAM.Permissions.Models;

public class Permission
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public string? Description { get; set; } = null!;
    public int AccountId { get; set; }
    public string ResourceType { get; set; } = null!;
    public int ResourceId { get; set; }
    public bool Create { get; set; }
    public bool Read { get; set; }
    public bool Update { get; set; }
    public bool Delete { get; set; }
    public bool Execute { get; set; }
}
