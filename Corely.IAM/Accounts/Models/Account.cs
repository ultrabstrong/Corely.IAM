using Corely.IAM.Models;
using Corely.IAM.Security.Models;

namespace Corely.IAM.Accounts.Models;

public class Account
{
    public const string RESOURCE_TYPE = "account";
    public const int ACCOUNT_NAME_MIN_LENGTH = 3;
    public const int ACCOUNT_NAME_MAX_LENGTH = 100;

    public Guid Id { get; init; }
    public string AccountName { get; init; } = null!;
    public bool IsPlatformAccount { get; init; }
    public List<SymmetricKey>? SymmetricKeys { get; set; } = null!;
    public List<AsymmetricKey>? AsymmetricKeys { get; set; } = null!;
    public List<ChildRef>? Users { get; set; }
    public List<ChildRef>? Groups { get; set; }
    public List<ChildRef>? Roles { get; set; }
    public List<ChildRef>? Permissions { get; set; }
}
