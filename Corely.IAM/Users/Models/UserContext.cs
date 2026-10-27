using Corely.IAM.Accounts.Models;
using Corely.IAM.ApiKeys.Models;
using Corely.IAM.Security.Models;

namespace Corely.IAM.Users.Models;

public record UserContext
{
    public User? User { get; init; }
    public ApiKey? ApiKey { get; init; }
    public Account? CurrentAccount { get; init; }
    public string DeviceId { get; init; }
    public Guid? AuthTokenId { get; init; }
    public List<Account> AvailableAccounts { get; init; }
    public bool IsSystemContext { get; init; }
    public bool EnteredAsPlatformMember { get; init; }

    internal AccountScope CurrentAccountScope =>
        IsSystemContext && CurrentAccount == null
            ? AccountScope.Any
            : AccountScope.Of(CurrentAccount?.Id ?? Guid.Empty);

    public List<Account> MemberAccounts =>
        EnteredAsPlatformMember
            ? [.. AvailableAccounts.Where(a => a.Id != CurrentAccount?.Id)]
            : AvailableAccounts;

    public UserContext(
        User user,
        Account? currentAccount,
        string deviceId,
        List<Account> availableAccounts,
        Guid? authTokenId = null,
        bool enteredAsPlatformMember = false
    )
    {
        User = user;
        CurrentAccount = currentAccount;
        DeviceId = deviceId;
        AuthTokenId = authTokenId;
        AvailableAccounts = availableAccounts;
        IsSystemContext = false;
        EnteredAsPlatformMember = enteredAsPlatformMember;
    }

    public UserContext(ApiKey apiKey, Account account, string deviceId, Guid? authTokenId = null)
    {
        ApiKey = apiKey;
        CurrentAccount = account;
        DeviceId = deviceId;
        AuthTokenId = authTokenId;
        AvailableAccounts = [account];
        IsSystemContext = false;
    }

    public UserContext(bool isSystemContext, string deviceId)
    {
        IsSystemContext = isSystemContext;
        DeviceId = deviceId;
        AuthTokenId = null;
        AvailableAccounts = [];
    }
}
