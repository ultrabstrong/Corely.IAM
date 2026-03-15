using Corely.Common.Extensions;
using Corely.IAM.Security.Providers;
using Corely.IAM.TotpAuths.Models;

namespace Corely.IAM.Services;

internal class MfaServiceAuthorizationDecorator(
    IMfaService inner,
    IAuthorizationProvider authorizationProvider
) : IMfaService
{
    private readonly IMfaService _inner = inner.ThrowIfNull(nameof(inner));
    private readonly IAuthorizationProvider _authorizationProvider =
        authorizationProvider.ThrowIfNull(nameof(authorizationProvider));

    public async Task<EnableTotpResult> EnableTotpAsync() =>
        _authorizationProvider.HasUserContext()
            ? await _inner.EnableTotpAsync()
            : new EnableTotpResult(
                EnableTotpResultCode.UnauthorizedError,
                "Unauthorized",
                null,
                null,
                null
            );

    public async Task<ConfirmTotpResult> ConfirmTotpAsync(ConfirmTotpRequest request) =>
        _authorizationProvider.HasUserContext()
            ? await _inner.ConfirmTotpAsync(request)
            : new ConfirmTotpResult(ConfirmTotpResultCode.UnauthorizedError, "Unauthorized");

    public async Task<DisableTotpResult> DisableTotpAsync(DisableTotpRequest request) =>
        _authorizationProvider.HasUserContext()
            ? await _inner.DisableTotpAsync(request)
            : new DisableTotpResult(DisableTotpResultCode.UnauthorizedError, "Unauthorized");

    public async Task<RegenerateTotpRecoveryCodesResult> RegenerateTotpRecoveryCodesAsync() =>
        _authorizationProvider.HasUserContext()
            ? await _inner.RegenerateTotpRecoveryCodesAsync()
            : new RegenerateTotpRecoveryCodesResult(
                RegenerateTotpRecoveryCodesResultCode.UnauthorizedError,
                "Unauthorized",
                null
            );

    public async Task<TotpStatusResult> GetTotpStatusAsync() =>
        _authorizationProvider.HasUserContext()
            ? await _inner.GetTotpStatusAsync()
            : new TotpStatusResult(
                TotpStatusResultCode.UnauthorizedError,
                "Unauthorized",
                false,
                0
            );
}
