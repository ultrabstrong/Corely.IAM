using Corely.Common.Extensions;
using Corely.IAM.GoogleAuths.Models;
using Corely.IAM.GoogleAuths.Processors;
using Corely.IAM.Users.Providers;

namespace Corely.IAM.Services;

internal class GoogleAuthService(
    IGoogleAuthProcessor googleAuthProcessor,
    IUserContextProvider userContextProvider
) : IGoogleAuthService
{
    private readonly IGoogleAuthProcessor _googleAuthProcessor = googleAuthProcessor.ThrowIfNull(
        nameof(googleAuthProcessor)
    );
    private readonly IUserContextProvider _userContextProvider = userContextProvider.ThrowIfNull(
        nameof(userContextProvider)
    );

    public async Task<LinkGoogleAuthResult> LinkGoogleAuthAsync(LinkGoogleAuthRequest request)
    {
        ArgumentNullException.ThrowIfNull(request, nameof(request));
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
