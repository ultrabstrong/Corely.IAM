using Corely.Common.Extensions;
using Corely.IAM.TotpAuths.Models;
using Corely.IAM.TotpAuths.Processors;
using Corely.IAM.Users.Providers;
using Corely.IAM.Validators;

namespace Corely.IAM.Services;

internal class MfaService(
    ITotpAuthProcessor totpAuthProcessor,
    IUserContextProvider userContextProvider,
    IValidationProvider validationProvider
) : IMfaService
{
    private readonly ITotpAuthProcessor _totpAuthProcessor = totpAuthProcessor.ThrowIfNull(
        nameof(totpAuthProcessor)
    );
    private readonly IUserContextProvider _userContextProvider = userContextProvider.ThrowIfNull(
        nameof(userContextProvider)
    );
    private readonly IValidationProvider _validationProvider = validationProvider.ThrowIfNull(
        nameof(validationProvider)
    );

    public async Task<EnableTotpResult> EnableTotpAsync()
    {
        var context = _userContextProvider.GetUserContext()!;

        return await _totpAuthProcessor.EnableTotpAsync(
            context.User!.Id,
            "Corely.IAM",
            context.User.Email
        );
    }

    public async Task<ConfirmTotpResult> ConfirmTotpAsync(ConfirmTotpRequest request)
    {
        ArgumentNullException.ThrowIfNull(request, nameof(request));
        var validation = _validationProvider.ValidateAndLog(request);
        if (!validation.IsValid)
        {
            return new ConfirmTotpResult(
                ConfirmTotpResultCode.InvalidCodeError,
                validation.Message
            );
        }

        var context = _userContextProvider.GetUserContext()!;

        return await _totpAuthProcessor.ConfirmTotpAsync(context.User!.Id, request.Code);
    }

    public async Task<DisableTotpResult> DisableTotpAsync(DisableTotpRequest request)
    {
        ArgumentNullException.ThrowIfNull(request, nameof(request));
        var validation = _validationProvider.ValidateAndLog(request);
        if (!validation.IsValid)
        {
            return new DisableTotpResult(
                DisableTotpResultCode.InvalidCodeError,
                validation.Message
            );
        }

        var context = _userContextProvider.GetUserContext()!;

        return await _totpAuthProcessor.DisableTotpAsync(context.User!.Id, request.Code);
    }

    public async Task<RegenerateTotpRecoveryCodesResult> RegenerateTotpRecoveryCodesAsync()
    {
        var context = _userContextProvider.GetUserContext()!;

        return await _totpAuthProcessor.RegenerateTotpRecoveryCodesAsync(context.User!.Id);
    }

    public async Task<TotpStatusResult> GetTotpStatusAsync()
    {
        var context = _userContextProvider.GetUserContext()!;

        return await _totpAuthProcessor.GetTotpStatusAsync(context.User!.Id);
    }
}
