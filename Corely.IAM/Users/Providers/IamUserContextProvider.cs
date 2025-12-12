using Corely.Common.Extensions;
using Corely.IAM.Security.Providers;
using Corely.IAM.Users.Models;

namespace Corely.IAM.Users.Providers;

internal class IamUserContextProvider(IAuthenticationProvider authenticationProvider)
    : IIamUserContextProvider,
        IIamUserContextSetter
{
    private readonly IAuthenticationProvider _authenticationProvider =
        authenticationProvider.ThrowIfNull(nameof(authenticationProvider));
    private IamUserContext? _userContext;

    public IamUserContext? GetUserContext() => _userContext;

    public async Task<UserAuthTokenValidationResultCode> SetUserContextAsync(string authToken)
    {
        ArgumentNullException.ThrowIfNull(authToken, nameof(authToken));

        var validationResult = await _authenticationProvider.ValidateUserAuthTokenAsync(authToken);

        if (
            validationResult.ResultCode != UserAuthTokenValidationResultCode.Success
            || !validationResult.UserId.HasValue
        )
        {
            return validationResult.ResultCode;
        }

        _userContext = new IamUserContext(
            validationResult.UserId.Value,
            validationResult.SignedInAccountId
        );
        return UserAuthTokenValidationResultCode.Success;
    }

    public void SetUserContext(IamUserContext context) => _userContext = context;
}
