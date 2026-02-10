using Corely.IAM.Permissions;

namespace Corely.IAM.Models;

public class EffectivePermission
{
    public Guid PermissionId { get; init; }
    public bool Create { get; init; }
    public bool Read { get; init; }
    public bool Update { get; init; }
    public bool Delete { get; init; }
    public bool Execute { get; init; }
    public string? Description { get; init; }
    public string ResourceType { get; init; } = null!;
    public Guid ResourceId { get; init; }
    public List<EffectiveRole> Roles { get; init; } = [];

    public string CrudxLabel =>
        PermissionLabelProvider.GetCrudxLabel(Create, Read, Update, Delete, Execute);
}
