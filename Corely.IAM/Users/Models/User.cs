using Corely.IAM.Models;
using Corely.IAM.Security.Models;

namespace Corely.IAM.Users.Models;

public class User
{
    public Guid Id { get; set; }
    public string Username { get; set; } = null!;
    public string Email { get; set; } = null!;
    public bool Disabled { get; set; }
    public int TotalSuccessfulLogins { get; set; }
    public DateTime? LastSuccessfulLoginUtc { get; set; }
    public int FailedLoginsSinceLastSuccess { get; set; }
    public int TotalFailedLogins { get; set; }
    public DateTime? LastFailedLoginUtc { get; set; }
    public DateTime CreatedUtc { get; set; }
    public DateTime? ModifiedUtc { get; set; }
    public List<SymmetricKey>? SymmetricKeys { get; set; } = null!;
    public List<AsymmetricKey>? AsymmetricKeys { get; set; } = null!;
    public List<ChildRef>? Accounts { get; set; }
    public List<ChildRef>? Groups { get; set; }
    public List<ChildRef>? Roles { get; set; }
}
