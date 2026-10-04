namespace Corely.IAM.Audits.Models;

[Flags]
public enum AuditActions
{
    None = 0,
    Create = 1,
    Read = 2,
    Update = 4,
    Delete = 8,
    Execute = 16,
    All = Create | Read | Update | Delete | Execute,
}
