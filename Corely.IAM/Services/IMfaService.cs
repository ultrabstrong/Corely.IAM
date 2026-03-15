using Corely.IAM.TotpAuths.Models;

namespace Corely.IAM.Services;

public interface IMfaService
{
    Task<EnableTotpResult> EnableTotpAsync();
    Task<ConfirmTotpResult> ConfirmTotpAsync(ConfirmTotpRequest request);
    Task<DisableTotpResult> DisableTotpAsync(DisableTotpRequest request);
    Task<RegenerateTotpRecoveryCodesResult> RegenerateTotpRecoveryCodesAsync();
    Task<TotpStatusResult> GetTotpStatusAsync();
}
