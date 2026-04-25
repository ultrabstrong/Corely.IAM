using Corely.Common.Extensions;
using Corely.IAM.GoogleAuths.Models;
using Corely.IAM.GoogleAuths.Processors;
using Corely.IAM.Users.Providers;
using Corely.IAM.Validators;

namespace Corely.IAM.Services;

internal class GoogleAuthService(
    IGoogleAuthProcessor googleAuthProcessor,
    IUserContextProvider userContextProvider,
    IValidationProvider validationProvider
) : IGoogleAuthService
{
    private readonly IGoogleAuthProcessor _googleAuthProcessor = googleAuthProcessor.ThrowIfNull(
        nameof(googleAuthProcessor)
    );
    private readonly IUserContextProvider _userContextProvider = userContextProvider.ThrowIfNull(
        nameof(userContextProvider)
    );
    private readonly IValidationProvider _validationProvider = validationProvider.ThrowIfNull(
        nameof(validationProvider)
    );

    public async Task<LinkGoogleAuthResult> LinkGoogleAuthAsync(LinkGoogleAuthRequest request)
    {
        ArgumentNullException.ThrowIfNull(request, nameof(request));
        var validation = _validationProvider.ValidateAndLog(request);
        if (!validation.IsValid)
        {
            return new LinkGoogleAuthResult(
                LinkGoogleAuthResultCode.InvalidGoogleTokenError,
                validation.Message
            );
        }

        var context = _userContextProvider.GetUserContext()!;

        return await _googleAuthProcessor.LinkGoogleAuthAsync(
            context.User!.Id,
            request.GoogleIdToken
        );
    }

    public async Task<UnlinkGoogleAuthResult> UnlinkGoogleAuthAsync()
    {
        var context = _userContextProvider.GetUserContext()!;

        return await _googleAuthProcessor.UnlinkGoogleAuthAsync(context.User!.Id);
    }

    public async Task<AuthMethodsResult> GetAuthMethodsAsync()
    {
        var context = _userContextProvider.GetUserContext()!;

        return await _googleAuthProcessor.GetAuthMethodsAsync(context.User!.Id);
    }
}
