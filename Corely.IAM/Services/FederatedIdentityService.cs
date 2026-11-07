using Corely.Common.Extensions;
using Corely.DataAccess.Interfaces.Repos;
using Corely.IAM.BasicAuths.Entities;
using Corely.IAM.FederatedIdentities.Models;
using Corely.IAM.FederatedIdentities.Processors;
using Corely.IAM.IdentityProviders.Enums;
using Corely.IAM.Security.Providers;
using Corely.IAM.Users.Providers;

namespace Corely.IAM.Services;

internal class FederatedIdentityService(
    IFederatedIdentityProcessor federatedIdentityProcessor,
    IReadonlyRepo<BasicAuthEntity> basicAuthRepo,
    IUserContextProvider userContextProvider,
    IAuthenticationProvider authenticationProvider
) : IFederatedIdentityService
{
    private readonly IFederatedIdentityProcessor _federatedIdentityProcessor =
        federatedIdentityProcessor.ThrowIfNull(nameof(federatedIdentityProcessor));
    private readonly IReadonlyRepo<BasicAuthEntity> _basicAuthRepo = basicAuthRepo.ThrowIfNull(
        nameof(basicAuthRepo)
    );
    private readonly IUserContextProvider _userContextProvider = userContextProvider.ThrowIfNull(
        nameof(userContextProvider)
    );
    private readonly IAuthenticationProvider _authenticationProvider =
        authenticationProvider.ThrowIfNull(nameof(authenticationProvider));

    public async Task<AuthMethodsResult> GetAuthMethodsAsync()
    {
        var userId = _userContextProvider.GetUserContext()!.User!.Id;
        return new AuthMethodsResult(
            AuthMethodsResultCode.Success,
            string.Empty,
            await _basicAuthRepo.AnyAsync(b => b.UserId == userId),
            await _federatedIdentityProcessor.ListAsync(userId)
        );
    }

    public async Task<UnlinkFederatedIdentityResult> UnlinkFederatedIdentityAsync(
        IdentityProviderType type
    )
    {
        var context = _userContextProvider.GetUserContext()!;
        var result = await _federatedIdentityProcessor.UnlinkAsync(context.User!.Id, type);
        if (result.ResultCode == UnlinkFederatedIdentityResultCode.Success)
        {
            await _authenticationProvider.RevokeAllUserAuthTokensAsync(
                context.User.Id,
                context.AuthTokenId
            );
        }
        return result;
    }
}
