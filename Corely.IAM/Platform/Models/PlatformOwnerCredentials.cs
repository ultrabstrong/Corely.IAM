namespace Corely.IAM.Platform.Models;

public record PlatformOwnerCredentials(
    Guid AccountId,
    string AccountName,
    string Username,
    string Email,
    string Password,
    string TotpSecret,
    string TotpSetupUri,
    IReadOnlyList<string> RecoveryCodes
);
