using Corely.Common.Extensions;
using Corely.IAM.TotpAuths.Models;
using Corely.IAM.TotpAuths.Processors;
using Corely.IAM.Users.Providers;

namespace Corely.IAM.Services;

internal class MfaService(
    ITotpAuthProcessor totpAuthProcessor,
    IUserContextProvider userContextProvider
) : IMfaService
{
    private readonly ITotpAuthProcessor _totpAuthProcessor = totpAuthProcessor.ThrowIfNull(
        nameof(totpAuthProcessor)
    );
    private readonly IUserContextProvider _userContextProvider = userContextProvider.ThrowIfNull(
        nameof(userContextProvider)
    );

    public async Task<EnableTotpResult> EnableTotpAsync()
    {
        var context = _userContextProvider.GetUserContext();
        return await _totpAuthProcessor.EnableTotpAsync(
            context!.User.Id,
            "Corely.IAM",
            context.User.Email
        );
    }

    public async Task<ConfirmTotpResult> ConfirmTotpAsync(ConfirmTotpRequest request)
    {
        ArgumentNullException.ThrowIfNull(request, nameof(request));
        var context = _userContextProvider.GetUserContext();
        return await _totpAuthProcessor.ConfirmTotpAsync(context!.User.Id, request.Code);
    }

    public async Task<DisableTotpResult> DisableTotpAsync(DisableTotpRequest request)
    {
        ArgumentNullException.ThrowIfNull(request, nameof(request));
        var context = _userContextProvider.GetUserContext();
        return await _totpAuthProcessor.DisableTotpAsync(context!.User.Id, request.Code);
    }

    public async Task<RegenerateTotpRecoveryCodesResult> RegenerateTotpRecoveryCodesAsync()
    {
        var context = _userContextProvider.GetUserContext();
        return await _totpAuthProcessor.RegenerateTotpRecoveryCodesAsync(context!.User.Id);
    }

    public async Task<TotpStatusResult> GetTotpStatusAsync()
    {
        var context = _userContextProvider.GetUserContext();
        return await _totpAuthProcessor.GetTotpStatusAsync(context!.User.Id);
    }
}
