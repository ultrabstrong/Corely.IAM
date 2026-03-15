using Corely.Common.Extensions;
using Corely.IAM.Security.Providers;
using Corely.IAM.TotpAuths.Models;

namespace Corely.IAM.TotpAuths.Processors;

internal class TotpAuthProcessorAuthorizationDecorator(
    ITotpAuthProcessor inner,
    IAuthorizationProvider authorizationProvider
) : ITotpAuthProcessor
{
    private readonly ITotpAuthProcessor _inner = inner.ThrowIfNull(nameof(inner));
    private readonly IAuthorizationProvider _authorizationProvider =
        authorizationProvider.ThrowIfNull(nameof(authorizationProvider));

    public async Task<EnableTotpResult> EnableTotpAsync(
        Guid userId,
        string issuer,
        string userLabel
    ) =>
        _authorizationProvider.IsAuthorizedForOwnUser(userId)
            ? await _inner.EnableTotpAsync(userId, issuer, userLabel)
            : new EnableTotpResult(
                EnableTotpResultCode.UnauthorizedError,
                "Unauthorized",
                null,
                null,
                null
            );

    public async Task<ConfirmTotpResult> ConfirmTotpAsync(Guid userId, string code) =>
        _authorizationProvider.IsAuthorizedForOwnUser(userId)
            ? await _inner.ConfirmTotpAsync(userId, code)
            : new ConfirmTotpResult(ConfirmTotpResultCode.UnauthorizedError, "Unauthorized");

    public async Task<DisableTotpResult> DisableTotpAsync(Guid userId, string code) =>
        _authorizationProvider.IsAuthorizedForOwnUser(userId)
            ? await _inner.DisableTotpAsync(userId, code)
            : new DisableTotpResult(DisableTotpResultCode.UnauthorizedError, "Unauthorized");

    public async Task<TotpStatusResult> GetTotpStatusAsync(Guid userId) =>
        _authorizationProvider.IsAuthorizedForOwnUser(userId)
            ? await _inner.GetTotpStatusAsync(userId)
            : new TotpStatusResult(
                TotpStatusResultCode.UnauthorizedError,
                "Unauthorized",
                false,
                0
            );

    public async Task<RegenerateTotpRecoveryCodesResult> RegenerateTotpRecoveryCodesAsync(
        Guid userId
    ) =>
        _authorizationProvider.IsAuthorizedForOwnUser(userId)
            ? await _inner.RegenerateTotpRecoveryCodesAsync(userId)
            : new RegenerateTotpRecoveryCodesResult(
                RegenerateTotpRecoveryCodesResultCode.UnauthorizedError,
                "Unauthorized",
                null
            );

    public Task<VerifyTotpOrRecoveryCodeResult> VerifyTotpOrRecoveryCodeAsync(
        VerifyTotpOrRecoveryCodeRequest request
    )
    {
        // No authorization required - called during MFA verification
        // before the user has a full authenticated context
        return _inner.VerifyTotpOrRecoveryCodeAsync(request);
    }

    public Task<bool> IsTotpEnabledAsync(Guid userId)
    {
        // No authorization required - called by AuthenticationService during sign-in
        return _inner.IsTotpEnabledAsync(userId);
    }
}
