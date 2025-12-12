using Corely.IAM.Accounts.Models;

namespace Corely.IAM.Users.Models;

internal record UserAuthTokenResult(string Token, List<Account> Accounts, int? SignedInAccountId);
