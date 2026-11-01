namespace Corely.IAM.Security.Models;

public sealed class AccountKeyProof(
    string? currentPassword,
    string? totpCode,
    string? googleIdToken = null,
    string? googleNonce = null
)
{
    internal string? CurrentPassword { get; } = currentPassword;
    internal string? TotpCode { get; } = totpCode;
    internal string? GoogleIdToken { get; } = googleIdToken;
    internal string? GoogleNonce { get; } = googleNonce;
}
