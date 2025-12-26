using Corely.IAM.Accounts.Models;

namespace Corely.IAM.Users.Models;

public record UserContext(int UserId, int? AccountId, string DeviceId, List<Account> Accounts);
