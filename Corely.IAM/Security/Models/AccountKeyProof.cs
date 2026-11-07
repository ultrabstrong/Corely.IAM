namespace Corely.IAM.Security.Models;

public sealed class AccountKeyProof(string? currentPassword, string? totpCode)
{
    internal string? CurrentPassword { get; } = currentPassword;
    internal string? TotpCode { get; } = totpCode;
}
