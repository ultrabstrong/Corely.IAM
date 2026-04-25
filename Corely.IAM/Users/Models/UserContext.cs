using Corely.IAM.Accounts.Models;

namespace Corely.IAM.Users.Models;

public record UserContext
{
    public User? User { get; init; }
    public Account? CurrentAccount { get; init; }
    public string DeviceId { get; init; }
    public List<Account> AvailableAccounts { get; init; }
    public bool IsSystemContext { get; init; }

    public UserContext(
        User user,
        Account? currentAccount,
        string deviceId,
        List<Account> availableAccounts
    )
    {
        User = user;
        CurrentAccount = currentAccount;
        DeviceId = deviceId;
        AvailableAccounts = availableAccounts;
        IsSystemContext = false;
    }

    public UserContext(bool isSystemContext, string deviceId)
    {
        IsSystemContext = isSystemContext;
        DeviceId = deviceId;
        AvailableAccounts = [];
    }
}
