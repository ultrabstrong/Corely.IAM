namespace Corely.IAM.Entities;

internal class UserAccount
{
    public Guid UsersId { get; set; }
    public Guid AccountsId { get; set; }
}

internal class UserGroup
{
    public Guid UsersId { get; set; }
    public Guid GroupsId { get; set; }
}

internal class UserRole
{
    public Guid UsersId { get; set; }
    public Guid RolesId { get; set; }
}

internal class GroupRole
{
    public Guid GroupsId { get; set; }
    public Guid RolesId { get; set; }
}

internal class ApiKeyGroup
{
    public Guid ApiKeysId { get; set; }
    public Guid GroupsId { get; set; }
}

internal class ApiKeyRole
{
    public Guid ApiKeysId { get; set; }
    public Guid RolesId { get; set; }
}

internal class RolePermission
{
    public Guid RolesId { get; set; }
    public Guid PermissionsId { get; set; }
}
