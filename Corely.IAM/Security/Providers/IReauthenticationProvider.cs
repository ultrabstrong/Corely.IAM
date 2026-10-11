using Corely.IAM.Security.Models;

namespace Corely.IAM.Security.Providers;

internal interface IReauthenticationProvider
{
    Task<ReauthenticationResult> VerifyCurrentPasswordAsync(Guid userId, string? currentPassword);
    Task<ReauthenticationResult> VerifyTotpCodeAsync(Guid userId, string? totpCode);
}
