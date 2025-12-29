namespace Corely.IAM.Permissions.Models;

public class Permission
{
    public Guid Id { get; set; }
    public string? Description { get; set; }
    public Guid AccountId { get; set; }
    public string ResourceType { get; set; } = null!;
    public Guid ResourceId { get; set; }
    public bool Create { get; set; }
    public bool Read { get; set; }
    public bool Update { get; set; }
    public bool Delete { get; set; }
    public bool Execute { get; set; }

    public string DisplayName =>
        $"{ResourceType} - {(ResourceId == Guid.Empty ? "all" : ResourceId)} {CrudxString}";

    private string CrudxString =>
        $"{(Create ? "C" : "c")}{(Read ? "R" : "r")}{(Update ? "U" : "u")}{(Delete ? "D" : "d")}{(Execute ? "X" : "x")}";
}
