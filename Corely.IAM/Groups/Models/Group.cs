namespace Corely.IAM.Groups.Models;

public class Group
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public string? Description { get; set; } = null!;
    public Guid AccountId { get; set; }
}
