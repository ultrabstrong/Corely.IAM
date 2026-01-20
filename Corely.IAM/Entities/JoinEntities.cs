namespace Corely.IAM.Entities;

public class UserAccount
{
    public Guid UsersId { get; set; }
    public Guid AccountsId { get; set; }
}

public class UserGroup
{
    public Guid UsersId { get; set; }
    public Guid GroupsId { get; set; }
}

public class UserRole
{
    public Guid UsersId { get; set; }
    public Guid RolesId { get; set; }
}

public class GroupRole
{
    public Guid GroupsId { get; set; }
    public Guid RolesId { get; set; }
}

public class RolePermission
{
    public Guid RolesId { get; set; }
    public Guid PermissionsId { get; set; }
}
