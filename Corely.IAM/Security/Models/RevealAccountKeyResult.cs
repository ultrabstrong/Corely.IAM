namespace Corely.IAM.Security.Models;

public enum RevealAccountKeyResultCode
{
    Success,
    UnauthorizedError,
    NotFoundError,
    BasicAuthNotFoundError,
    InvalidCurrentPasswordError,
    UserLockedError,
    TotpRequiredError,
    InvalidTotpCodeError,
}

public sealed class RevealAccountKeyResult(
    RevealAccountKeyResultCode resultCode,
    string message,
    string? key = null,
    string? providerName = null,
    int version = 0
)
{
    private readonly string? _key = key;

    public RevealAccountKeyResultCode ResultCode { get; } = resultCode;
    public string Message { get; } = message;
    public string? ProviderName { get; } = providerName;
    public int Version { get; } = version;

    public string? ReadKey() => _key;
}
