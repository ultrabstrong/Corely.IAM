using Corely.IAM.Accounts.Models;

namespace Corely.IAM.Users.Models;

public record UserContext(
    User User,
    Account? CurrentAccount,
    string DeviceId,
    List<Account> AvailableAccounts
);
