namespace Corely.IAM.Groups.Models;

public class Group
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public string? Description { get; set; } = null!;
    public int AccountId { get; set; }
}
