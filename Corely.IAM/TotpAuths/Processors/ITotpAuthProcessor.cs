using Corely.IAM.TotpAuths.Models;

namespace Corely.IAM.TotpAuths.Processors;

internal interface ITotpAuthProcessor
{
    Task<EnableTotpResult> EnableTotpAsync(Guid userId, string issuer, string userLabel);
    Task<ConfirmTotpResult> ConfirmTotpAsync(Guid userId, string code);
    Task<DisableTotpResult> DisableTotpAsync(Guid userId, string code);
    Task<TotpStatusResult> GetTotpStatusAsync(Guid userId);
    Task<RegenerateTotpRecoveryCodesResult> RegenerateTotpRecoveryCodesAsync(Guid userId);
    Task<VerifyTotpOrRecoveryCodeResult> VerifyTotpOrRecoveryCodeAsync(
        VerifyTotpOrRecoveryCodeRequest request
    );
    Task<bool> IsTotpEnabledAsync(Guid userId);
}
