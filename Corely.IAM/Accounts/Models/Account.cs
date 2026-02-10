using Corely.IAM.Models;
using Corely.IAM.Security.Models;

namespace Corely.IAM.Accounts.Models;

public class Account
{
    public Guid Id { get; init; }
    public string AccountName { get; init; } = null!;
    public List<SymmetricKey>? SymmetricKeys { get; set; } = null!;
    public List<AsymmetricKey>? AsymmetricKeys { get; set; } = null!;
    public List<ChildRef>? Users { get; set; }
    public List<ChildRef>? Groups { get; set; }
    public List<ChildRef>? Roles { get; set; }
    public List<ChildRef>? Permissions { get; set; }
}
